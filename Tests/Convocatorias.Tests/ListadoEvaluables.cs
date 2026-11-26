using System.Net.Http.Json;
using DELTAAPI.Models;
using Xunit;
namespace Convocatorias.Tests;
public class ListadoEvaluables
{
    [Fact]
    public async Task Filtra_Roles_Sin_Mezclar_Puestos_Y_Destinos()
    {
        using var f=new ApiFactory();using var c=f.Client();c.DefaultRequestHeaders.Add("Cookie",await f.Cookie("Inspector"));
        await f.WithDb(async db=>{
            db.Usuarios.Add(new Usuario{IdUsuario=20,NombreCompleto="Evaluado",Ci="20",Contraseña="sin-login",Rol="Usuario",TipoEvaluado="Empleado",PuestoActual="Analista",PuestoARotar="Estabilidad",Estado="Activo"});
            db.Usuarios.Add(new Usuario{IdUsuario=21,NombreCompleto="Admin",Ci="21",Contraseña="sin-login",Rol="Administrador"});
            await db.SaveChangesAsync();return true;
        });
        var filtrados=await c.GetFromJsonAsync<List<UserViewModel>>("api/usuarios?soloEvaluables=true");
        var unico=Assert.Single(filtrados!);Assert.Equal(20,unico.IdUsuario);Assert.Equal("Analista",unico.PuestoActual);Assert.Equal("Estabilidad",unico.PuestoARotar);Assert.Equal("Usuario",unico.Rol);
        var todos=await c.GetFromJsonAsync<List<UserViewModel>>("api/usuarios");Assert.Equal(3,todos!.Count);
        Assert.Contains(todos,u=>u.Rol=="Inspector");
    }
}
