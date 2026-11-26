using System.ComponentModel.DataAnnotations;
using System.Net;
using System.Net.Http.Json;
using System.Text.Json;
using Delta.Contracts;
using Microsoft.EntityFrameworkCore;
using Xunit;
namespace Convocatorias.Tests;
public class ConvocatoriaAsistida
{
    private static ConvocatoriaForm Form() => new() {
        NombreConvocatoria="Evaluación",Resumen="Resumen",Descripcion="Descripción",Objetivo="Objetivo",Participantes="Empleados",
        Requisitos="Cuenta activa",Modalidad="Teórica",Instrucciones="Instrucciones",Lugar="DELTATEST",Contacto="Inspector responsable",
        FechaConvocatoria=new DateOnly(2026,1,1),FechaCierre=new DateOnly(2026,1,5),FechaEvaluacion=new DateOnly(2026,1,6),HoraInicio=new TimeOnly(9,0),HoraFin=new TimeOnly(10,30)
    };
    [Fact]
    public async Task Asigna_Codigos_Distintos_Y_Conserva_El_Cronograma_Al_Editar()
    {
        using var f=new ApiFactory();using var c=f.Client();c.DefaultRequestHeaders.Add("Cookie",await f.Cookie("Inspector"));c.DefaultRequestHeaders.Add("X-Requested-With","XMLHttpRequest");
        var codigos=new HashSet<string>();
        for(var i=0;i<2;i++) {
            var response=await c.PostAsJsonAsync("api/convocatorias",Form());Assert.Equal(HttpStatusCode.Created,response.StatusCode);
            using var json=JsonDocument.Parse(await response.Content.ReadAsStringAsync());
            var codigo=json.RootElement.GetProperty("codigo").GetString()!;Assert.Matches("^CONV-[0-9]{4}-[0-9A-F]{12}$",codigo);Assert.True(codigos.Add(codigo));
            var id=json.RootElement.GetProperty("idConvocatoria").GetInt32();
            var detalle=(await c.GetFromJsonAsync<ConvocatoriaForm>($"api/convocatorias/{id}"))!;
            Assert.True(detalle.CargarCronograma());Assert.Equal(new TimeOnly(10,30),detalle.HoraFin);Assert.Equal(Form().FechaEvaluacion,detalle.FechaEvaluacion);
            Assert.Equal(codigo,detalle.Codigo);detalle.NombreConvocatoria="Actualizada";
            Assert.Equal(HttpStatusCode.NoContent,(await c.PutAsJsonAsync($"api/convocatorias/{id}",detalle)).StatusCode);
            await f.WithDb(async db=>{var fila=await db.Convocatoria.SingleAsync(x=>x.IdConvocatoria==id);Assert.Equal(codigo,fila.Codigo);Assert.Equal(Form().Cronograma,fila.Cronograma);return true;});
        }
    }
    [Theory]
    [InlineData(8,30,false)]
    [InlineData(9,0,false)]
    [InlineData(10,30,true)]
    public void Valida_Hora_De_Fin(int hora,int minuto,bool valido)
    {
        var form=Form();form.HoraFin=new TimeOnly(hora,minuto);
        Assert.Equal(valido,Validator.TryValidateObject(form,new ValidationContext(form),new List<ValidationResult>(),true));
    }
    [Fact]
    public void Cronograma_Anterior_No_Se_Modifica_Al_Cargar()
    {
        var form=new ConvocatoriaForm{Cronograma="Horario por confirmar con los responsables."};
        Assert.False(form.CargarCronograma());Assert.Equal("Horario por confirmar con los responsables.",form.Cronograma);Assert.Null(form.FechaEvaluacion);
    }
    [Fact]
    public void Rechaza_Fecha_Anterior_A_Publicacion_Y_Horario_Incompleto()
    {
        var form=Form();form.FechaEvaluacion=new DateOnly(2025,12,31);
        Assert.False(Validator.TryValidateObject(form,new ValidationContext(form),new List<ValidationResult>(),true));
        form=Form();form.HoraFin=null;
        Assert.False(Validator.TryValidateObject(form,new ValidationContext(form),new List<ValidationResult>(),true));
    }
}
