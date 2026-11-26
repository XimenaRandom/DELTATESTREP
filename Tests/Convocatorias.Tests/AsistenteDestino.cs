using System.Net;
using System.Text;
using System.Text.Json;
using DELTAAPI.Controllers;
using DELTAAPI.Models;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging.Abstractions;
using Xunit;

namespace Convocatorias.Tests;

public class AsistenteDestino
{
    private sealed class GeminiDoble : HttpMessageHandler, IHttpClientFactory
    {
        public string? Prompt;
        public int Llamadas;
        public HttpClient CreateClient(string name) => new(this, false);
        protected override async Task<HttpResponseMessage> SendAsync(HttpRequestMessage request, CancellationToken ct)
        {
            Llamadas++;
            using var body = JsonDocument.Parse(await request.Content!.ReadAsStringAsync(ct));
            Assert.False(body.RootElement.GetProperty("generationConfig").TryGetProperty("thinkingConfig", out _));
            Prompt = body.RootElement.GetProperty("contents")[0].GetProperty("parts")[0].GetProperty("text").GetString();
            var examen = JsonSerializer.Serialize(new {
                preguntasAbiertas = new[] { new { texto = "Explique el procedimiento del puesto.", puntos = 10 } },
                preguntasMultiples = new[] { new { texto = "Seleccione el procedimiento correcto.", puntos = 10,
                    opciones = new[] { new { texto = "Correcto", esCorrecta = true }, new { texto = "Incorrecto", esCorrecta = false } } } }
            });
            return new HttpResponseMessage(HttpStatusCode.OK) {
                Content = new StringContent(JsonSerializer.Serialize(new {
                    candidates = new[] { new { content = new { parts = new[] { new { text = examen } } } } }
                }), Encoding.UTF8, "application/json")
            };
        }
    }

    [Theory]
    [InlineData("Empleado", "Validaciones analiticas", "Farmacovigilancia", "Validaciones analiticas", 200)]
    [InlineData("Pasante", "Documentacion", "Control de calidad", "Control de calidad", 200)]
    [InlineData("Empleado", null, "No usar", null, 400)]
    [InlineData("Pasante", "No usar", "  ", null, 400)]
    [InlineData(null, "No usar", "No usar", null, 400)]
    public async Task Resuelve_Destino_Servidor_Y_No_Envia_Identidad(string? tipo, string? rotar, string? solicitado, string? esperado, int status)
    {
        await using var db = Contexto();
        db.Usuarios.Add(new Usuario { IdUsuario = 20, NombreCompleto = "NO_ENVIAR_NOMBRE", Ci = "NO_ENVIAR_CI",
            Contraseña = "sin-login", Correo = "privado@example.invalid", Rol = "Usuario", Estado = "Activo",
            TipoEvaluado = tipo, PuestoActual = "NO_USAR_PUESTO_ACTUAL", PuestoARotar = rotar, PuestoSolicitado = solicitado });
        await db.SaveChangesAsync();
        using var gemini = new GeminiDoble();
        var resultado = Assert.IsAssignableFrom<ObjectResult>(await Controlador(db, gemini).GenerarPreguntasIA(
            new() { IdUsuario = 20, Subarea = "TEMA_MANIPULADO", CantidadAbiertas = 1, CantidadMultiples = 1 }));
        Assert.Equal(status, resultado.StatusCode);
        if (status != 200) { Assert.Equal(0, gemini.Llamadas); return; }
        using var json = JsonDocument.Parse(JsonSerializer.Serialize(resultado.Value));
        Assert.Equal(esperado, json.RootElement.GetProperty("subarea").GetString());
        Assert.Contains(esperado!, gemini.Prompt);
        foreach (var privado in new[] { "TEMA_MANIPULADO", "NO_USAR_PUESTO_ACTUAL", "NO_ENVIAR_NOMBRE", "NO_ENVIAR_CI", "privado@example.invalid" })
            Assert.DoesNotContain(privado, gemini.Prompt);
        Assert.Equal(1, gemini.Llamadas);
    }

    [Theory]
    [InlineData(0, 400)]
    [InlineData(999, 404)]
    public async Task Valida_Usuario_Antes_De_Consultar_IA(int id, int status)
    {
        await using var db = Contexto();
        using var gemini = new GeminiDoble();
        var result = Assert.IsAssignableFrom<ObjectResult>(await Controlador(db, gemini).GenerarPreguntasIA(new() { IdUsuario = id }));
        Assert.Equal(status, result.StatusCode);
        Assert.Equal(0, gemini.Llamadas);
    }

    [Theory]
    [InlineData("Inspector")]
    [InlineData("Administrador")]
    public async Task No_Genera_Examen_Para_Roles_De_Gestion(string rol)
    {
        await using var db = Contexto(); using var gemini = new GeminiDoble();
        db.Usuarios.Add(new Usuario { IdUsuario=20, NombreCompleto="Gestor", Ci="20", Contraseña="sin-login", Rol=rol, TipoEvaluado="Empleado", PuestoARotar="Estabilidad" });
        await db.SaveChangesAsync();
        Assert.IsType<BadRequestObjectResult>(await Controlador(db,gemini).GenerarPreguntasIA(new(){IdUsuario=20}));
        Assert.Equal(0,gemini.Llamadas);
    }

    private static DeltaTestContext Contexto() => new(new DbContextOptionsBuilder<DeltaTestContext>()
        .UseInMemoryDatabase(Guid.NewGuid().ToString()).Options);
    private static EvaluacionesController Controlador(DeltaTestContext db, IHttpClientFactory gemini) => new(db, gemini,
        new ConfigurationBuilder().AddInMemoryCollection(new Dictionary<string,string?> { ["GEMINI_API_KEY"] = "prueba-sin-red" }).Build(),
        NullLogger<EvaluacionesController>.Instance);
}
