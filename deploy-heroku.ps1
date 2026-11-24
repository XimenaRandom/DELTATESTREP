param([string]$App = "delta-prod")
$ErrorActionPreference = "Stop"
Set-Location $PSScriptRoot
function Invoke-Heroku {
    & heroku @args
    if ($LASTEXITCODE -ne 0) { throw "Falló heroku $($args[0]); despliegue detenido." }
}
if (-not (Get-Command heroku -ErrorAction SilentlyContinue)) {
    throw "Instala Heroku CLI: https://devcenter.heroku.com/articles/heroku-cli y ejecuta heroku login."
}
$changes = & git status --porcelain
if ($LASTEXITCODE -ne 0 -or $changes) {
    throw "Guarda y confirma los cambios del repositorio con git add y git commit antes de desplegar."
}
Invoke-Heroku auth:whoami
Invoke-Heroku apps:info -a $App
$config = Invoke-Heroku config --json -a $App | ConvertFrom-Json
if ([string]::IsNullOrWhiteSpace($config.ConnectionStrings__DefaultConnection)) {
    throw "Configura ConnectionStrings__DefaultConnection con la conexión Npgsql de Supabase en Heroku Settings > Config Vars."
}
Invoke-Heroku config:set ASPNETCORE_ENVIRONMENT=Production 'AllowedHosts=*' Auth__CookieSameSite=Lax ClientBaseUrl=/ -a $App
Invoke-Heroku stack:set container -a $App
Invoke-Heroku git:remote -a $App
& git push heroku HEAD:main
if ($LASTEXITCODE -ne 0) { throw "Falló el build o release. Revisa heroku logs --tail -a $App" }
Invoke-Heroku ps:scale web=1 -a $App
Invoke-Heroku open -a $App
