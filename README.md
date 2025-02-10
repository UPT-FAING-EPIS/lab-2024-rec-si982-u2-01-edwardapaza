[![Review Assignment Due Date](https://classroom.github.com/assets/deadline-readme-button-22041afd0340ce965d47ae6ef1cefeee28c7c493a6346c4f15d667ab976d596c.svg)](https://classroom.github.com/a/pA-AbV7A)
[![Open in Codespaces](https://classroom.github.com/assets/launch-codespace-2972f46106e565e64193e422d61a12cf1da4916b45550586e14ef0a7c637dd04.svg)](https://classroom.github.com/open-in-codespaces?assignment_repo_id=18011964)
# SESION DE LABORATORIO N° 02: Construyendo una Aplicación Web con ASP.NET y Entity Framework

## OBJETIVOS
  * Comprender el desarrollo una Aplicación Web utilizando ASP.NET y Entity Framework

## REQUERIMIENTOS
  * Conocimientos: 
    - Conocimientos básicos de SQL.
    - Conocimientos shell y comandos en modo terminal.
  * Hardware:
    - Virtualization activada en el BIOS.
    - CPU SLAT-capable feature.
    - Al menos 4GB de RAM.
  * Software:
    - Windows 10 64bit: Pro, Enterprise o Education (1607 Anniversary Update, Build 14393 o Superior)
    - Docker Desktop 
    - Powershell versión 7.x
    - .Net 8
    - Azure CLI

## CONSIDERACIONES INICIALES
  * Tener una cuenta en Infracost (https://www.infracost.io/), sino utilizar su cuenta de github para generar su cuenta y generar un token.
  * Tener una cuenta en SonarCloud (https://sonarcloud.io/), sino utilizar su cuenta de github para generar su cuenta y generar un token. El token debera estar registrado en su repositorio de Github con el nombre de SONAR_TOKEN. 
  * Tener una cuenta con suscripción en Azure (https://portal.azure.com/). Tener el ID de la Suscripción, que se utilizará en el laboratorio
  * Clonar el repositorio mediante git para tener los recursos necesarios en una ubicación que no sea restringida del sistema.

## DESARROLLO

### PREPARACION DE LA INFRAESTRUCTURA

1. Iniciar la aplicación Powershell o Windows Terminal en modo administrador, ubicarse en ua ruta donde se ha realizado la clonación del repositorio
```Powershell
md infra
```
2. Abrir Visual Studio Code, seguidamente abrir la carpeta del repositorio clonado del laboratorio, en el folder Infra, crear el archivo main.tf con el siguiente contenido
```Terraform
terraform {
  required_providers {
    azurerm = {
      source  = "hashicorp/azurerm"
      version = "~> 4.0.0"
    }
  }
  required_version = ">= 0.14.9"
}

variable "suscription_id" {
    type = string
    description = "Azure subscription id"
}

variable "sqladmin_username" {
    type = string
    description = "Administrator username for server"
}

variable "sqladmin_password" {
    type = string
    description = "Administrator password for server"
}

provider "azurerm" {
  features {}
  subscription_id = var.suscription_id
}

# Generate a random integer to create a globally unique name
resource "random_integer" "ri" {
  min = 100
  max = 999
}

# Create the resource group
resource "azurerm_resource_group" "rg" {
  name     = "upt-arg-${random_integer.ri.result}"
  location = "eastus"
}

# Create the Linux App Service Plan
resource "azurerm_service_plan" "appserviceplan" {
  name                = "upt-asp-${random_integer.ri.result}"
  location            = azurerm_resource_group.rg.location
  resource_group_name = azurerm_resource_group.rg.name
  os_type             = "Linux"
  sku_name            = "F1"
}

# Create the web app, pass in the App Service Plan ID
resource "azurerm_linux_web_app" "webapp" {
  name                  = "upt-awa-${random_integer.ri.result}"
  location              = azurerm_resource_group.rg.location
  resource_group_name   = azurerm_resource_group.rg.name
  service_plan_id       = azurerm_service_plan.appserviceplan.id
  depends_on            = [azurerm_service_plan.appserviceplan]
  //https_only            = true
  site_config {
    minimum_tls_version = "1.2"
    always_on = false
    application_stack {
      docker_image_name = "patrickcuadros/shorten:latest"
      docker_registry_url = "https://index.docker.io"      
    }
  }
}

resource "azurerm_mssql_server" "sqlsrv" {
  name                         = "upt-dbs-${random_integer.ri.result}"
  resource_group_name          = azurerm_resource_group.rg.name
  location                     = azurerm_resource_group.rg.location
  version                      = "12.0"
  administrator_login          = var.sqladmin_username
  administrator_login_password = var.sqladmin_password
}

resource "azurerm_mssql_firewall_rule" "sqlaccessrule" {
  name             = "PublicAccess"
  server_id        = azurerm_mssql_server.sqlsrv.id
  start_ip_address = "0.0.0.0"
  end_ip_address   = "255.255.255.255"
}

resource "azurerm_mssql_database" "sqldb" {
  name      = "shorten"
  server_id = azurerm_mssql_server.sqlsrv.id
  sku_name = "Free"
}
```
![image](https://github.com/user-attachments/assets/988083eb-29b8-464e-a62a-732e46c6b4df)

3. Abrir un navegador de internet y dirigirse a su repositorio en Github, en la sección *Settings*, buscar la opción *Secrets and Variables* y seleccionar la opción *Actions*. Dentro de esta crear los siguientes secretos
> AZURE_USERNAME: Correo o usuario de cuenta de Azure
> AZURE_PASSWORD: Password de cuenta de Azure
> SUSCRIPTION_ID: ID de la Suscripción de cuenta de Azure
> SQL_USER: Usuario administrador de la base de datos, ejm: adminsql
> SQL_PASS: Password del usuario administrador de la base de datos, ejm: upt.2025

![image](https://github.com/user-attachments/assets/efe2f029-63c4-437a-b339-89a6bd4f475f)

5. En el Visual Studio Code, crear la carpeta .github/workflows en la raiz del proyecto, seguidamente crear el archivo deploy.yml con el siguiente contenido
<details><summary>Click to expand: deploy.yml</summary>

```Yaml
name: Construcción infrastructura en Azure

on:
  push:
    branches: [ "main" ]
    paths:
      - 'infra/**'
      - '.github/workflows/infra.yml'
  workflow_dispatch:

jobs:
  Deploy-infra:
    runs-on: ubuntu-latest
    steps:
      - uses: actions/checkout@v4
      - name: login azure
        run: | 
          az login -u ${{ secrets.AZURE_USERNAME }} -p ${{ secrets.AZURE_PASSWORD }}

      - name: Create terraform.tfvars
        run: |
          cd infra
          echo "suscription_id=\"${{ secrets.SUSCRIPTION_ID }}\"" > terraform.tfvars
          echo "sqladmin_username=\"${{ secrets.SQL_USER }}\"" >> terraform.tfvars
          echo "sqladmin_password=\"${{ secrets.SQL_PASS }}\"" >> terraform.tfvars

      # - name: Setup tfsec
      #   run: |
      #       curl -L -o /tmp/tfsec_1.28.13_linux_amd64.tar.gz "https://github.com/aquasecurity/tfsec/releases/download/v1.28.13/tfsec_1.28.13_linux_amd64.tar.gz"
      #       tar -xzvf /tmp/tfsec_1.28.13_linux_amd64.tar.gz -C /tmp
      #       mv -v /tmp/tfsec /usr/local/bin/tfsec
      #       chmod +x /usr/local/bin/tfsec
      # - name: tfsec
      #   run: |
      #     cd infra
      #     /usr/local/bin/tfsec --format=markdown --tfvars-file=terraform.tfvars --out=tfsec.md .
      #     echo "## TFSec Output" >> $GITHUB_STEP_SUMMARY
      #     cat tfsec.md >> $GITHUB_STEP_SUMMARY
  
      - name: Setup Terraform
        uses: hashicorp/setup-terraform@v3
      - name: Terraform Init
        id: init
        run: cd infra && terraform init 
    #   - name: Terraform Fmt
    #     id: fmt
    #     run: cd infra && terraform fmt -check
      - name: Terraform Validate
        id: validate
        run: cd infra && terraform validate -no-color
      - name: Terraform Plan
        run: cd infra && terraform plan -var="suscription_id=${{ secrets.SUSCRIPTION_ID }}" -var="sqladmin_username=${{ secrets.SQL_USER }}" -var="sqladmin_password=${{ secrets.SQL_PASS }}" -no-color -out main.tfplan

      - name: Create String Output
        id: tf-plan-string
        run: |
            TERRAFORM_PLAN=$(cd infra && terraform show -no-color main.tfplan)

            delimiter="$(openssl rand -hex 8)"
            echo "summary<<${delimiter}" >> $GITHUB_OUTPUT
            echo "## Terraform Plan Output" >> $GITHUB_OUTPUT
            echo "<details><summary>Click to expand</summary>" >> $GITHUB_OUTPUT
            echo "" >> $GITHUB_OUTPUT
            echo '```terraform' >> $GITHUB_OUTPUT
            echo "$TERRAFORM_PLAN" >> $GITHUB_OUTPUT
            echo '```' >> $GITHUB_OUTPUT
            echo "</details>" >> $GITHUB_OUTPUT
            echo "${delimiter}" >> $GITHUB_OUTPUT

      - name: Publish Terraform Plan to Task Summary
        env:
          SUMMARY: ${{ steps.tf-plan-string.outputs.summary }}
        run: |
          echo "$SUMMARY" >> $GITHUB_STEP_SUMMARY

      - name: Outputs
        id: vars
        run: |
            echo "terramaid_version=$(curl -s https://api.github.com/repos/RoseSecurity/Terramaid/releases/latest | grep tag_name | cut -d '"' -f 4)" >> $GITHUB_OUTPUT
            case "${{ runner.arch }}" in
            "X64" )
                echo "arch=x86_64" >> $GITHUB_OUTPUT
                ;;
            "ARM64" )
                echo "arch=arm64" >> $GITHUB_OUTPUT
                ;;
            esac

      - name: Setup Go
        uses: actions/setup-go@v5
        with:
          go-version: 'stable'

      - name: Setup Terramaid
        run: |
            curl -L -o /tmp/terramaid.tar.gz "https://github.com/RoseSecurity/Terramaid/releases/download/${{ steps.vars.outputs.terramaid_version }}/Terramaid_Linux_${{ steps.vars.outputs.arch }}.tar.gz"
            tar -xzvf /tmp/terramaid.tar.gz -C /tmp
            mv -v /tmp/Terramaid /usr/local/bin/terramaid
            chmod +x /usr/local/bin/terramaid

      - name: Terramaid
        id: terramaid
        run: |
            cd infra
            /usr/local/bin/terramaid run

      - name: Publish graph in step comment
        run: |
            echo "## Terramaid Graph" >> $GITHUB_STEP_SUMMARY
            cat infra/Terramaid.md >> $GITHUB_STEP_SUMMARY 

      - name: Setup Graphviz
        uses: ts-graphviz/setup-graphviz@v2        

      - name: Setup inframap
        run: |
            curl -L -o /tmp/inframap.tar.gz "https://github.com/cycloidio/inframap/releases/download/v0.7.0/inframap-linux-amd64.tar.gz"
            tar -xzvf /tmp/inframap.tar.gz -C /tmp
            mv -v /tmp/inframap-linux-amd64 /usr/local/bin/inframap
            chmod +x /usr/local/bin/inframap
      - name: inframap
        run: |
            cd infra
            /usr/local/bin/inframap generate main.tf --raw | dot -Tsvg > inframap_azure.svg
      - name: Upload inframap
        id: inframap-upload-step
        uses: actions/upload-artifact@v4
        with:
          name: inframap_azure.svg
          path: infra/inframap_azure.svg

      - name: Setup infracost
        uses: infracost/actions/setup@v3
        with:
            api-key: ${{ secrets.INFRACOST_API_KEY }}
      - name: infracost
        run: |
            cd infra
            infracost breakdown --path . --format html --out-file infracost-report.html
            sed -i '19,137d' infracost-report.html
            sed -i 's/$0/$ 0/g' infracost-report.html

      - name: Convert HTML to Markdown
        id: html2markdown
        uses: rknj/html2markdown@v1.1.0
        with:
            html-file: "infra/infracost-report.html"

      - name: Upload infracost report
        run: |
            echo "## infracost Report" >> $GITHUB_STEP_SUMMARY
            echo "${{ steps.html2markdown.outputs.markdown-content }}" >> infracost.md
            cat infracost.md >> $GITHUB_STEP_SUMMARY

      - name: Terraform Apply
        run: |
            cd infra
            terraform apply -var="suscription_id=${{ secrets.SUSCRIPTION_ID }}" -var="sqladmin_username=${{ secrets.SQL_USER }}" -var="sqladmin_password=${{ secrets.SQL_PASS }}" -auto-approve main.tfplan
```
</details>

6. En el Visual Studio Code, guardar los cambios y subir los cambios al repositorio. Revisar los logs de la ejeuciòn de automatizaciòn y anotar el numero de identificaciòn de Grupo de Recursos y Aplicación Web creados
```Bash
azurerm_linux_web_app.webapp: Creation complete after 53s [id=/subscriptions/1f57de72-50fd-4271-8ab9-3fc129f02bc0/resourceGroups/upt-arg-XXX/providers/Microsoft.Web/sites/upt-awa-XXX]
```

### CONSTRUCCION DE LA APLICACION

1. En el terminal, ubicarse en un ruta que no sea del sistema y ejecutar los siguientes comandos.
```Bash
dotnet new webapp -o src -n Shorten
cd src
dotnet tool install -g dotnet-aspnet-codegenerator --version 8.0.0
dotnet add package Microsoft.AspNetCore.Identity.UI --version 8.0.0
dotnet add package Microsoft.AspNetCore.Identity.EntityFrameworkCore --version 8.0.0
dotnet add package Microsoft.EntityFrameworkCore.Design --version=8.0.0
dotnet add package Microsoft.EntityFrameworkCore.SqlServer --version=8.0.0
dotnet add package Microsoft.EntityFrameworkCore.Tools --version=8.0.0
dotnet add package Microsoft.VisualStudio.Web.CodeGeneration.Design --version=8.0.0
dotnet add package Microsoft.AspNetCore.Components.QuickGrid --version=8.0.0
dotnet add package Microsoft.AspNetCore.Components.QuickGrid.EntityFrameworkAdapter --version=8.0.0
```
![image](https://github.com/user-attachments/assets/1db473c4-3131-4e94-921c-6a366f46fb6a)

2. En el terminal, ejecutar el siguiente comando para crear los modelos de autenticación de identidad dentro de la aplicación.
```Bash
dotnet aspnet-codegenerator identity --useDefaultUI
```

3. En el VS Code, modificar la cadena de conexión de la base de datos en el archivo appsettings.json, de la siguiente manera:
```JSon
"ShortenIdentityDbContextConnection": "Server=tcp:upt-dbs-XXX.database.windows.net,1433;Initial Catalog=shorten;Persist Security Info=False;User ID=YYY;Password=ZZZ;MultipleActiveResultSets=False;Encrypt=True;TrustServerCertificate=False;Connection Timeout=30;"
```
>Donde: XXX, id de su servidor de base de datos
>       YYY, usuario administrador de base de datos
>       ZZZ, password del usuario de base de datos

4. En el terminal, ejecutar el siguiente comando para crear las tablas de base de datos de identidad.
```Bash
dotnet ef migrations add CreateIdentitySchema
dotnet ef database update
```

5. En el Visual Studio Code, en la carpeta src/Areas/Domain, crear el archivo UrlMapping.cs con el siguiente contenido:
```CSharp
namespace Shorten.Areas.Domain;
/// <summary>
/// Clase de dominio que representa una acortaciòn de url
/// </summary>
public class UrlMapping
{
    /// <summary>
    /// Identificador del mapeo de url
    /// </summary>
    /// <value>Entero</value>
    public int Id { get; set; }
    /// <summary>
    /// Valor original de la url
    /// </summary>
    /// <value>Cadena</value>
    public string OriginalUrl { get; set; } = string.Empty;
    /// <summary>
    /// Valor corto de la url
    /// </summary>
    /// <value>Cadena</value>
    public string ShortenedUrl { get; set; } = string.Empty;
}
```
![image](https://github.com/user-attachments/assets/938fca99-595d-4de7-9c2f-997655d986d6)

6. En el Visual Studio Code, en la carpeta src/Areas/Domain, crear el archivo ShortenContext.cs con el siguiente contenido:
```CSharp
using Microsoft.EntityFrameworkCore;
namespace Shorten.Models;
/// <summary>
/// Clase de infraestructura que representa el contexto de la base de datos
/// </summary>
using Microsoft.EntityFrameworkCore;
namespace Shorten.Areas.Domain;
/// <summary>
/// Clase de infraestructura que representa el contexto de la base de datos
/// </summary>
public class ShortenContext : DbContext
{
    /// <summary>
    /// Constructor de la clase
    /// </summary>
    /// <param name="options">opciones de conexiòn de BD</param>
    public ShortenContext(DbContextOptions<ShortenContext> options) : base(options)
    {
    }
  
    /// <summary>
    /// Propiedad que representa la tabla de mapeo de urls
    /// </summary>
    /// <value>Conjunto de UrlMapping</value>
    public DbSet<UrlMapping> UrlMappings { get; set; }
}
```
![image](https://github.com/user-attachments/assets/117191aa-4f52-4aed-a325-b986ebb45f70)

7. En el Visual Studio Code, en la carpeta src, modificar el archivo Program.cs con el siguiente contenido al inicio:
```CSharp
using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;
using Shorten.Areas.Identity.Data;
using Shorten.Areas.Domain;
var builder = WebApplication.CreateBuilder(args);
var connectionString = builder.Configuration.GetConnectionString("ShortenIdentityDbContextConnection") ?? throw new InvalidOperationException("Connection string 'ShortenIdentityDbContextConnection' not found.");

builder.Services.AddDbContext<ShortenIdentityDbContext>(options => options.UseSqlServer(connectionString));

builder.Services.AddDefaultIdentity<IdentityUser>(options => options.SignIn.RequireConfirmedAccount = true).AddEntityFrameworkStores<ShortenIdentityDbContext>();

builder.Services.AddDbContext<ShortenContext>(options => options.UseSqlServer(connectionString));
builder.Services.AddQuickGridEntityFrameworkAdapter();

// Add services to the container.
builder.Services.AddRazorPages();

var app = builder.Build();

// Configure the HTTP request pipeline.
if (!app.Environment.IsDevelopment())
{
    app.UseExceptionHandler("/Error");
    // The default HSTS value is 30 days. You may want to change this for production scenarios, see https://aka.ms/aspnetcore-hsts.
    app.UseHsts();
}

app.UseHttpsRedirection();
app.UseStaticFiles();

app.UseRouting();

app.UseAuthorization();

app.MapRazorPages();

app.Run();
```
![image](https://github.com/user-attachments/assets/4000979c-7755-4565-88aa-853cfc90a290)

8. En el terminal, ejecutar los siguientes comandos para realizar la migración de la entidad UrlMapping
```Powershell
dotnet ef migrations add DomainModel --context ShortenContext
dotnet ef database update --context ShortenContext
```

9. En el terminal, ejecutar el siguiente comando para crear nu nuevo controlador y sus vistas asociadas.
```Powershell
dotnet aspnet-codegenerator razorpage Index List -m UrlMapping -dc ShortenContext -outDir Pages/UrlMapping -udl
dotnet aspnet-codegenerator razorpage Create Create -m UrlMapping -dc ShortenContext -outDir Pages/UrlMapping -udl
dotnet aspnet-codegenerator razorpage Edit Edit -m UrlMapping -dc ShortenContext -outDir Pages/UrlMapping -udl
dotnet aspnet-codegenerator razorpage Delete Delete -m UrlMapping -dc ShortenContext -outDir Pages/UrlMapping -udl
dotnet aspnet-codegenerator razorpage Details Details -m UrlMapping -dc ShortenContext -outDir Pages/UrlMapping -udl
```
![image](https://github.com/user-attachments/assets/066d46f8-5b3e-4c21-b6f6-ae57ef388565)

10. En el Visual Studio Code, en la carpeta src, modificar el archivo _Layout.cshtml, Adicionando la siguiente opciòn dentro del navegador:
```CSharp
<!DOCTYPE html>
<html lang="en">
<head>
    <meta charset="utf-8" />
    <meta name="viewport" content="width=device-width, initial-scale=1.0" />
    <title>@ViewData["Title"] - Shorten</title>
    <link rel="stylesheet" href="~/lib/bootstrap/dist/css/bootstrap.min.css" />
    <link rel="stylesheet" href="~/css/site.css" asp-append-version="true" />
    <link rel="stylesheet" href="~/Shorten.styles.css" asp-append-version="true" />
</head>
<body>
    <header>
        <nav class="navbar navbar-expand-sm navbar-toggleable-sm navbar-light bg-white border-bottom box-shadow mb-3">
            <div class="container">
                <a class="navbar-brand" asp-area="" asp-page="/Index">Shorten</a>
                <button class="navbar-toggler" type="button" data-bs-toggle="collapse" data-bs-target=".navbar-collapse" aria-controls="navbarSupportedContent"
                        aria-expanded="false" aria-label="Toggle navigation">
                    <span class="navbar-toggler-icon"></span>
                </button>
                <div class="navbar-collapse collapse d-sm-inline-flex justify-content-between">
                    <ul class="navbar-nav flex-grow-1">
                        <li class="nav-item">
                            <a class="nav-link text-dark" asp-area="" asp-page="/Index">Home</a>
                        </li>
                        <li class="nav-item">
                            <a class="nav-link text-dark" asp-area="" asp-page="/Privacy">Privacy</a>
                        </li>
                        <li class="nav-item">
                            <a class="nav-link text-dark" asp-area="" asp-page="/UrlMapping/Index">Shorten</a>
                        </li>                    
                    </ul>
                    <partial name="_LoginPartial" />
                </div>
            </div>
        </nav>
    </header>
    <div class="container">
        <main role="main" class="pb-3">
            @RenderBody()
        </main>
    </div>

    <footer class="border-top footer text-muted">
        <div class="container">
            &copy; 2025 - Shorten - <a asp-area="" asp-page="/Privacy">Privacy</a>
        </div>
    </footer>

    <script src="~/lib/jquery/dist/jquery.min.js"></script>
    <script src="~/lib/bootstrap/dist/js/bootstrap.bundle.min.js"></script>
    <script src="~/js/site.js" asp-append-version="true"></script>

    @await RenderSectionAsync("Scripts", required: false)
</body>
</html>
```
11. En el Visual Studio Code, en la carpeta raiz del proyecto, crear un nuevo archivo Dockerfile con el siguiente contenido:
```Dockerfile
# Utilizar la imagen base de .NET SDK
FROM mcr.microsoft.com/dotnet/sdk:8.0 AS build

# Establecer el directorio de trabajo
WORKDIR /app

# Copiar el resto de la aplicación y compilar
COPY src/. ./
RUN dotnet restore
RUN dotnet publish -c Release -o out

# Utilizar la imagen base de .NET Runtime
FROM mcr.microsoft.com/dotnet/aspnet:8.0-alpine AS runtime
LABEL org.opencontainers.image.source="https://github.com/p-cuadros/Shorten02"

# Establecer el directorio de trabajo
WORKDIR /app
ENV ASPNETCORE_URLS=http://+:80
RUN apk add icu-libs
ENV DOTNET_SYSTEM_GLOBALIZATION_INVARIANT=false
# Copiar los archivos compilados desde la etapa de construcción
COPY --from=build /app/out .

# Definir el comando de entrada para ejecutar la aplicación
ENTRYPOINT ["dotnet", "Shorten.dll"]
``` 
![image](https://github.com/user-attachments/assets/9ac2f5d0-fdc1-426c-b97a-0eca8a4ff535)

### DESPLIEGUE DE LA APLICACION 

1. En el terminal, ejecutar el siguiente comando para obtener el perfil publico (Publish Profile) de la aplicación. Anotarlo porque se utilizara posteriormente.
```Powershell
az webapp deployment list-publishing-profiles --name upt-awa-XXX --resource-group upt-arg-XXX --xml
```
> Donde XXX; es el numero de identicación de la Aplicación Web creada en la primera sección
![image](https://github.com/user-attachments/assets/4bafa9e5-360e-43ca-a6ad-a1df0dae5c4e)

2. Abrir un navegador de internet y dirigirse a su repositorio en Github, en la sección *Settings*, buscar la opción *Secrets and Variables* y seleccionar la opción *Actions*. Dentro de esta hacer click en el botón *New Repository Secret*. En el navegador, dentro de la ventana *New Secret*, colocar como nombre AZURE_WEBAPP_PUBLISH_PROFILE y como valor el obtenido en el paso anterior.
 
3. En el Visual Studio Code, dentro de la carpeta `.github/workflows`, crear el archivo ci-cd.yml con el siguiente contenido
```Yaml
name: Construcción y despliegue de una aplicación MVC a Azure

env:
  AZURE_WEBAPP_NAME: upt-awa-XXX  # Aqui va el nombre de su aplicación
  DOTNET_VERSION: '8'                     # la versión de .NET

on:
  push:
    branches: [ "main" ]
    paths:
      - 'src/**'
      - '.github/workflows/**'
  workflow_dispatch:
permissions:
  contents: read
  packages: write
jobs:
  build:
    runs-on: ubuntu-latest
    steps:
      - uses: actions/checkout@v4
      - name: 'Login to GitHub Container Registry'
        uses: docker/login-action@v3
        with:
            registry: ghcr.io
            username: ${{github.actor}}
            password: ${{secrets.GITHUB_TOKEN}}

      - name: 'Build Inventory Image'
        run: |
            docker build . --tag ghcr.io/${{github.actor}}/shorten:${{github.sha}}
            docker push ghcr.io/${{github.actor}}/shorten:${{github.sha}}

  deploy:
    permissions:
      contents: none
    runs-on: ubuntu-latest
    needs: build
    environment:
      name: 'Development'
      url: ${{ steps.deploy-to-webapp.outputs.webapp-url }}

    steps:
      - name: Desplegar a Azure Web App
        id: deploy-to-webapp
        uses: azure/webapps-deploy@v2
        with:
          app-name: ${{ env.AZURE_WEBAPP_NAME }}
          publish-profile: ${{ secrets.AZURE_WEBAPP_PUBLISH_PROFILE }}
          images: ghcr.io/${{github.actor}}/shorten:${{github.sha}}
```

4. En el Visual Studio Code o en el Terminal, confirmar los cambios con sistema de controlde versiones (git add ... git commit...) y luego subir esos cambios al repositorio remoto (git push ...).
   
5. En el Navegador de internet, dirigirse al repositorio de Github y revisar la seccion Actions, verificar que se esta ejecutando correctamente el Workflow.

![image](https://github.com/user-attachments/assets/32524d42-09cf-4595-9733-413c8f8a02b1)

6. En el Navegador de internet, una vez finalizada la automatización, ingresar al sitio creado y navegar por el (https://upt-awa-XXX.azurewebsites.net).
![image](https://github.com/user-attachments/assets/878dc47f-bdc3-44f7-a151-1e0193edae57)

7. En el Terminal, revisar las metricas de navegacion con el siguiente comando.
```Powershell
az monitor metrics list --resource "/subscriptions/XXXXXXXXXXXXXXX/resourceGroups/upt-arg-XXX/providers/Microsoft.Web/sites/upt-awa-XXXX" --metric "Requests" --start-time 2025-01-07T18:00:00Z --end-time 2025-01-07T23:00:00Z --output table
```
```
tput table;78ff1589-ef80-4c29-8a4b-ca5cfbd8dde9Timestamp             Name      Total
--------------------  --------  -------
2025-02-10T18:00:00Z  Requests  0.0
2025-02-10T18:01:00Z  Requests  0.0
2025-02-10T18:02:00Z  Requests  0.0
2025-02-10T18:03:00Z  Requests  0.0
2025-02-10T18:04:00Z  Requests  0.0
2025-02-10T18:05:00Z  Requests  0.0
2025-02-10T18:06:00Z  Requests  0.0
2025-02-10T18:07:00Z  Requests  0.0
2025-02-10T18:08:00Z  Requests  0.0
2025-02-10T18:09:00Z  Requests  0.0
2025-02-10T18:10:00Z  Requests  0.0
2025-02-10T18:11:00Z  Requests  0.0
2025-02-10T18:12:00Z  Requests  0.0
2025-02-10T18:13:00Z  Requests  0.0
2025-02-10T18:14:00Z  Requests  0.0
2025-02-10T18:15:00Z  Requests  0.0
2025-02-10T18:16:00Z  Requests  0.0
2025-02-10T18:17:00Z  Requests  0.0
2025-02-10T18:18:00Z  Requests  0.0
2025-02-10T18:19:00Z  Requests  0.0
2025-02-10T18:20:00Z  Requests  0.0
2025-02-10T18:21:00Z  Requests  0.0
2025-02-10T18:22:00Z  Requests  0.0
2025-02-10T18:23:00Z  Requests  0.0
2025-02-10T18:24:00Z  Requests  0.0
2025-02-10T18:25:00Z  Requests  0.0
2025-02-10T18:26:00Z  Requests  0.0
2025-02-10T18:27:00Z  Requests  0.0
2025-02-10T18:28:00Z  Requests  0.0
2025-02-10T18:29:00Z  Requests  0.0
2025-02-10T18:30:00Z  Requests  0.0
2025-02-10T18:31:00Z  Requests  0.0
2025-02-10T18:32:00Z  Requests  0.0
2025-02-10T18:33:00Z  Requests  0.0
2025-02-10T18:34:00Z  Requests  0.0
2025-02-10T18:35:00Z  Requests  0.0
2025-02-10T18:36:00Z  Requests  0.0
2025-02-10T18:37:00Z  Requests  0.0
2025-02-10T18:38:00Z  Requests  0.0
2025-02-10T18:39:00Z  Requests  0.0
2025-02-10T18:40:00Z  Requests  0.0
2025-02-10T18:41:00Z  Requests  0.0
2025-02-10T18:42:00Z  Requests  0.0
2025-02-10T18:43:00Z  Requests  0.0
2025-02-10T18:44:00Z  Requests  0.0
2025-02-10T18:45:00Z  Requests  0.0
2025-02-10T18:46:00Z  Requests  0.0
2025-02-10T18:47:00Z  Requests  0.0
2025-02-10T18:48:00Z  Requests  0.0
2025-02-10T18:49:00Z  Requests  0.0
2025-02-10T18:50:00Z  Requests  0.0
2025-02-10T18:51:00Z  Requests  0.0
2025-02-10T18:52:00Z  Requests  0.0
2025-02-10T18:53:00Z  Requests  0.0
2025-02-10T18:54:00Z  Requests  0.0
2025-02-10T18:55:00Z  Requests  0.0
2025-02-10T18:56:00Z  Requests  0.0
2025-02-10T18:57:00Z  Requests  0.0
2025-02-10T18:58:00Z  Requests  0.0
2025-02-10T18:59:00Z  Requests  0.0
2025-02-10T19:00:00Z  Requests  0.0
2025-02-10T19:01:00Z  Requests  0.0
2025-02-10T19:02:00Z  Requests  0.0
2025-02-10T19:03:00Z  Requests  0.0
2025-02-10T19:04:00Z  Requests  0.0
2025-02-10T19:05:00Z  Requests  0.0
2025-02-10T19:06:00Z  Requests  0.0
2025-02-10T19:07:00Z  Requests  0.0
2025-02-10T19:08:00Z  Requests  0.0
2025-02-10T19:09:00Z  Requests  0.0
2025-02-10T19:10:00Z  Requests  0.0
2025-02-10T19:11:00Z  Requests  0.0
2025-02-10T19:12:00Z  Requests  0.0
2025-02-10T19:13:00Z  Requests  0.0
2025-02-10T19:14:00Z  Requests  0.0
2025-02-10T19:15:00Z  Requests  0.0
2025-02-10T19:16:00Z  Requests  0.0
2025-02-10T19:17:00Z  Requests  0.0
2025-02-10T19:18:00Z  Requests  0.0
2025-02-10T19:19:00Z  Requests  0.0
2025-02-10T19:20:00Z  Requests  0.0
2025-02-10T19:21:00Z  Requests  0.0
2025-02-10T19:22:00Z  Requests  0.0
2025-02-10T19:23:00Z  Requests  0.0
2025-02-10T19:24:00Z  Requests  0.0
2025-02-10T19:25:00Z  Requests  0.0
2025-02-10T19:26:00Z  Requests  0.0
2025-02-10T19:27:00Z  Requests  0.0
2025-02-10T19:28:00Z  Requests  0.0
2025-02-10T19:29:00Z  Requests  0.0
2025-02-10T19:30:00Z  Requests  0.0
2025-02-10T19:31:00Z  Requests  0.0
2025-02-10T19:32:00Z  Requests  0.0
2025-02-10T19:33:00Z  Requests  0.0
2025-02-10T19:34:00Z  Requests  0.0
2025-02-10T19:35:00Z  Requests  0.0
2025-02-10T19:36:00Z  Requests  0.0
2025-02-10T19:37:00Z  Requests  0.0
2025-02-10T19:38:00Z  Requests  0.0
2025-02-10T19:39:00Z  Requests  0.0
2025-02-10T19:40:00Z  Requests  0.0
2025-02-10T19:41:00Z  Requests  0.0
2025-02-10T19:42:00Z  Requests  0.0
2025-02-10T19:43:00Z  Requests  0.0
2025-02-10T19:44:00Z  Requests  0.0
2025-02-10T19:45:00Z  Requests  0.0
2025-02-10T19:46:00Z  Requests  0.0
2025-02-10T19:47:00Z  Requests  0.0
2025-02-10T19:48:00Z  Requests  0.0
2025-02-10T19:49:00Z  Requests  0.0
2025-02-10T19:50:00Z  Requests  0.0
2025-02-10T19:51:00Z  Requests  0.0
2025-02-10T19:52:00Z  Requests  0.0
2025-02-10T19:53:00Z  Requests  0.0
2025-02-10T19:54:00Z  Requests  0.0
2025-02-10T19:55:00Z  Requests  0.0
2025-02-10T19:56:00Z  Requests  0.0
2025-02-10T19:57:00Z  Requests  0.0
2025-02-10T19:58:00Z  Requests  0.0
2025-02-10T19:59:00Z  Requests  0.0
2025-02-10T20:00:00Z  Requests  0.0
2025-02-10T20:01:00Z  Requests  0.0
2025-02-10T20:02:00Z  Requests  0.0
2025-02-10T20:03:00Z  Requests  0.0
2025-02-10T20:04:00Z  Requests  0.0
2025-02-10T20:05:00Z  Requests  0.0
2025-02-10T20:06:00Z  Requests  0.0
2025-02-10T20:07:00Z  Requests  0.0
2025-02-10T20:08:00Z  Requests  0.0
2025-02-10T20:09:00Z  Requests  0.0
2025-02-10T20:10:00Z  Requests  0.0
2025-02-10T20:11:00Z  Requests  0.0
2025-02-10T20:12:00Z  Requests  0.0
2025-02-10T20:13:00Z  Requests  0.0
2025-02-10T20:14:00Z  Requests  0.0
2025-02-10T20:15:00Z  Requests  0.0
2025-02-10T20:16:00Z  Requests  0.0
2025-02-10T20:17:00Z  Requests  0.0
2025-02-10T20:18:00Z  Requests  0.0
2025-02-10T20:19:00Z  Requests  0.0
2025-02-10T20:20:00Z  Requests  0.0
2025-02-10T20:21:00Z  Requests  0.0
2025-02-10T20:22:00Z  Requests  0.0
2025-02-10T20:23:00Z  Requests  0.0
2025-02-10T20:24:00Z  Requests  0.0
2025-02-10T20:25:00Z  Requests  0.0
2025-02-10T20:26:00Z  Requests  0.0
2025-02-10T20:27:00Z  Requests  0.0
2025-02-10T20:28:00Z  Requests  0.0
2025-02-10T20:29:00Z  Requests  0.0
2025-02-10T20:30:00Z  Requests  0.0
2025-02-10T20:31:00Z  Requests  0.0
2025-02-10T20:32:00Z  Requests  0.0
2025-02-10T20:33:00Z  Requests  0.0
2025-02-10T20:34:00Z  Requests  0.0
2025-02-10T20:35:00Z  Requests  0.0
2025-02-10T20:36:00Z  Requests  0.0
2025-02-10T20:37:00Z  Requests  0.0
2025-02-10T20:38:00Z  Requests  0.0
2025-02-10T20:39:00Z  Requests  0.0
2025-02-10T20:40:00Z  Requests  0.0
2025-02-10T20:41:00Z  Requests  0.0
2025-02-10T20:42:00Z  Requests  0.0
2025-02-10T20:43:00Z  Requests  0.0
2025-02-10T20:44:00Z  Requests  0.0
2025-02-10T20:45:00Z  Requests  0.0
2025-02-10T20:46:00Z  Requests  0.0
2025-02-10T20:47:00Z  Requests  0.0
2025-02-10T20:48:00Z  Requests  0.0
2025-02-10T20:49:00Z  Requests  0.0
2025-02-10T20:50:00Z  Requests  0.0
2025-02-10T20:51:00Z  Requests  0.0
2025-02-10T20:52:00Z  Requests  0.0
2025-02-10T20:53:00Z  Requests  0.0
2025-02-10T20:54:00Z  Requests  0.0
2025-02-10T20:55:00Z  Requests  0.0
2025-02-10T20:56:00Z  Requests  0.0
2025-02-10T20:57:00Z  Requests  0.0
2025-02-10T20:58:00Z  Requests  0.0
2025-02-10T20:59:00Z  Requests  0.0
2025-02-10T21:00:00Z  Requests  0.0
2025-02-10T21:01:00Z  Requests  0.0
2025-02-10T21:02:00Z  Requests  0.0
2025-02-10T21:03:00Z  Requests  0.0
2025-02-10T21:04:00Z  Requests  0.0
2025-02-10T21:05:00Z  Requests  0.0
2025-02-10T21:06:00Z  Requests  0.0
2025-02-10T21:07:00Z  Requests  0.0
2025-02-10T21:08:00Z  Requests  0.0
2025-02-10T21:09:00Z  Requests  0.0
2025-02-10T21:10:00Z  Requests  0.0
2025-02-10T21:11:00Z  Requests  0.0
2025-02-10T21:12:00Z  Requests  0.0
2025-02-10T21:13:00Z  Requests  0.0
2025-02-10T21:14:00Z  Requests  0.0
2025-02-10T21:15:00Z  Requests  0.0
2025-02-10T21:16:00Z  Requests  0.0
2025-02-10T21:17:00Z  Requests  0.0
2025-02-10T21:18:00Z  Requests  0.0
2025-02-10T21:19:00Z  Requests  0.0
2025-02-10T21:20:00Z  Requests  0.0
2025-02-10T21:21:00Z  Requests  0.0
2025-02-10T21:22:00Z  Requests  0.0
2025-02-10T21:23:00Z  Requests  0.0
2025-02-10T21:24:00Z  Requests  0.0
2025-02-10T21:25:00Z  Requests  0.0
2025-02-10T21:26:00Z  Requests  0.0
2025-02-10T21:27:00Z  Requests  0.0
2025-02-10T21:28:00Z  Requests  0.0
2025-02-10T21:29:00Z  Requests  0.0
2025-02-10T21:30:00Z  Requests  0.0
2025-02-10T21:31:00Z  Requests  0.0
2025-02-10T21:32:00Z  Requests  0.0
2025-02-10T21:33:00Z  Requests  0.0
2025-02-10T21:34:00Z  Requests  0.0
2025-02-10T21:35:00Z  Requests  0.0
2025-02-10T21:36:00Z  Requests  0.0
2025-02-10T21:37:00Z  Requests  0.0
2025-02-10T21:38:00Z  Requests  10.0
2025-02-10T21:39:00Z  Requests  0.0
2025-02-10T21:40:00Z  Requests  0.0
2025-02-10T21:41:00Z  Requests  4.0
2025-02-10T21:42:00Z  Requests  0.0
2025-02-10T21:43:00Z  Requests  0.0
2025-02-10T21:44:00Z  Requests  0.0
```
> Reemplazar los valores: 1. ID de suscripcion de Azure, 2. ID de creaciòn de infra y 3. El rango de fechas de uso de la aplicación.

7. En el Terminal, ejecutar el siguiente comando para obtener la plantilla de los recursos creados de azure en el grupo de recursos UPT.
```Powershell
az group export -n upt-arg-XXX > lab_01.json
```
![image](https://github.com/user-attachments/assets/42e9cba0-2d26-4336-a738-83f1da7ed8c0)

8. En el Visual Studio Code, instalar la extensión *ARM Template Viewer*, abrir el archivo lab_02.json y hacer click en el icono de previsualizar ARM.


## ACTIVIDADES ENCARGADAS

1. Subir el diagrama al repositorio como lab_02.png y el reporte de metricas.
![image](https://github.com/user-attachments/assets/2b676b32-d7f0-4f47-913c-e7a88ce5e3eb)
![image](https://github.com/user-attachments/assets/b92e0db2-8c6c-4a53-80e1-d0fdd9ec872c)

3. Realizar el scanero del codigo de terraform utilizando TfSec o Trivy dentro del Github Action.
4. En la aplicación completar el envio de correo para el registro de usuarios (https://learn.microsoft.com/es-es/aspnet/core/security/authentication/accconfirm?view=aspnetcore-9.0&tabs=visual-studio)
5. En la aplicación migrar la cadena de conexion a la base de datos a una Configuración de aplicación de Azure, como una variable de ambiente.
6. Realizar el escaneo de vulnerabilidad con SonarCloud y Semgrep dentro del Github Action correspondiente.
