using System.ComponentModel.DataAnnotations;
using System.Net;
using System.Net.Http.Json;
using System.Text.Json;
using DELTAAPI.Models;
using Microsoft.EntityFrameworkCore;
using Xunit;
namespace Convocatorias.Tests;
public class EdicionCompleta
{
    [Fact]
    public async Task Guarda_Extension_Rol_Y_Vinculacion_Sin_Cambiar_Clave_Vacia()
    {
        using var f = new ApiFactory(); using var c = f.Client();
        c.DefaultRequestHeaders.Add("Cookie", await f.Cookie()); c.DefaultRequestHeaders.Add("X-Requested-With", "XMLHttpRequest");
        await f.WithDb(async db => { db.Usuarios.Add(new Usuario { IdUsuario=20, NombreCompleto="Prueba", Ci="200", Contraseña="hash-original", Rol="Usuario", Estado="Activo" }); await db.SaveChangesAsync(); return true; });
        var response = await c.PutAsJsonAsync("api/usuarios/20", new { expedicion="lp", rol="Inspector", tipoEvaluado="Empleado", puestoActual="Analista", puestoARotar="Documentacion", password="", confirmPassword="" });
        Assert.Equal(HttpStatusCode.NoContent,response.StatusCode);
        await f.WithDb(async db => { var u=(await db.Usuarios.FindAsync(20))!; Assert.Equal("LP",u.Expedicion); Assert.Equal("Inspector",u.Rol); Assert.Equal("Empleado",u.TipoEvaluado); Assert.Equal("Documentacion",u.PuestoARotar); Assert.Equal("hash-original",u.Contraseña); return true; });
        using var json=JsonDocument.Parse(await c.GetStringAsync("api/usuarios/20"));
        Assert.Equal("LP",json.RootElement.GetProperty("expedicion").GetString());
        Assert.False(json.RootElement.TryGetProperty("password",out _));
        Assert.False(json.RootElement.TryGetProperty("contraseña",out _));
    }

    [Theory]
    [InlineData("ClaveNueva2026!", "ClaveNueva2026!", "LP", 204)]
    [InlineData("corta", "corta", "LP", 400)]
    [InlineData("ClaveNueva2026!", "OtraClave2026!", "LP", 400)]
    [InlineData("", "ClaveNueva2026!", "LP", 400)]
    [InlineData("", "", "XX", 400)]
    public async Task Valida_Clave_Confirmacion_Y_Extension(string password,string confirmPassword,string expedicion,int status)
    {
        using var f=new ApiFactory();using var c=f.Client();c.DefaultRequestHeaders.Add("Cookie",await f.Cookie("Inspector"));c.DefaultRequestHeaders.Add("X-Requested-With","XMLHttpRequest");
        await f.WithDb(async db=>{db.Usuarios.Add(new Usuario{IdUsuario=20,NombreCompleto="Prueba",Ci="200",Contraseña="hash-original",Rol="Usuario",Estado="Activo"});await db.SaveChangesAsync();return true;});
        Assert.Equal(status,(int)(await c.PutAsJsonAsync("api/usuarios/20",new{password,confirmPassword,expedicion})).StatusCode);
        await f.WithDb(async db=>{var u=(await db.Usuarios.FindAsync(20))!;if(status==204){Assert.NotEqual(password,u.Contraseña);Assert.True(BCrypt.Net.BCrypt.Verify(password,u.Contraseña));}else Assert.Equal("hash-original",u.Contraseña);return true;});
    }

    [Fact]
    public async Task Perfil_Usuario_No_Puede_Resetear_Clave_Sin_Flujo_Autorizado()
    {
        using var f=new ApiFactory();using var c=f.Client();c.DefaultRequestHeaders.Add("Cookie",await f.Cookie("Usuario"));c.DefaultRequestHeaders.Add("X-Requested-With","XMLHttpRequest");
        Assert.Equal(HttpStatusCode.Forbidden,(await c.PutAsJsonAsync("api/usuarios/1",new{password="ClaveNueva2026!",confirmPassword="ClaveNueva2026!"})).StatusCode);
    }

    [Theory]
    [InlineData(null,null,true)]
    [InlineData("ClaveNueva2026!","ClaveNueva2026!",true)]
    [InlineData("corta","corta",false)]
    [InlineData("ClaveNueva2026!","OtraClave2026!",false)]
    public void Formulario_Valida_Clave_Opcional(string? clave,string? confirmacion,bool valido)
    {
        var model=new DELTATEST.Models.EditUsuarioModel{NombreCompleto="Prueba",Correo="prueba@example.invalid",Rol="Usuario",TipoEvaluado="Pasante",PuestoSolicitado="Analista",Password=clave,ConfirmPassword=confirmacion};
        Assert.Equal(valido,Validator.TryValidateObject(model,new ValidationContext(model),new List<ValidationResult>(),true));
    }
}
