# RentalHub

Plataforma de alquiler de inmuebles desarrollada en ASP.NET Core MVC sobre .NET 10, diseñada para facilitar la gestión de reservas, validación de identidad (KYC), administración de propiedades y análisis de rendimiento para propietarios.

---

## Tecnologías Utilizadas

- .NET 10
- ASP.NET Core MVC
- Entity Framework Core
- PostgreSQL
- ASP.NET Core Identity
- Docker
- Docker Compose
- ClosedXML
- OpenAI GPT-4o Vision (opcional)

---

## Requisitos Previos

Antes de ejecutar la aplicación asegúrese de tener instalado:

- Docker
- Docker Compose

No es necesario instalar PostgreSQL ni .NET SDK para ejecutar la solución mediante contenedores.

---

## Docker

### Construir e iniciar los servicios

```bash
docker compose up --build
```

### Ejecutar en segundo plano

```bash
docker compose up -d --build
```

### Detener los servicios

```bash
docker compose down
```

### Eliminar contenedores y volúmenes

```bash
docker compose down -v
```

### Acceso a la aplicación

Una vez iniciada la aplicación:

```text
http://localhost:8080
```

### Inicialización automática

Al iniciar el proyecto:

- Se crea la base de datos PostgreSQL.
- Se aplican las migraciones de Entity Framework Core.
- Se crean las tablas necesarias para el funcionamiento de la aplicación.

---

## Arquitectura de la Solución

RentalHub fue desarrollado utilizando ASP.NET Core MVC sobre .NET 10 siguiendo una separación clara de responsabilidades para facilitar el mantenimiento, escalabilidad y pruebas del sistema.

### Capas Principales

#### Controllers

Gestionan las solicitudes HTTP, validan entradas y coordinan la comunicación entre la interfaz de usuario y la lógica de negocio.

#### Services

Contienen la lógica de negocio desacoplada mediante interfaces e implementaciones concretas.

#### Models

Representan las entidades persistentes del dominio utilizadas por Entity Framework Core.

#### ViewModels

Modelos específicos para la presentación de datos entre controladores y vistas Razor.

#### Data

Contiene la configuración del contexto de base de datos y la integración con Entity Framework Core.

#### Areas

Se utilizó el área `Owner` para encapsular las funcionalidades exclusivas de los propietarios, manteniendo una separación clara respecto a las funcionalidades del huésped.

---

## Estructura del Proyecto

```text
RentalHub/
├── Areas/
│   └── Owner/
├── Constants/
├── Controllers/
├── Data/
├── Migrations/
├── Models/
├── Services/
│   ├── Interfaces/
│   └── Implementations/
├── ViewModels/
├── Views/
├── wwwroot/
├── Properties/
├── Program.cs
├── appsettings.json
├── appsettings.Development.json
├── Dockerfile
├── docker-compose.yml
├── .gitignore
└── README.md
```

---

## Decisiones Técnicas

### Prevención de Double Booking

Antes de confirmar una reserva, el sistema valida que no existan reservas confirmadas que se crucen con el rango de fechas solicitado para un mismo inmueble, garantizando la disponibilidad real y evitando conflictos operativos.

### Horarios Estandarizados

Todas las reservas utilizan automáticamente:

- Check-in: 2:00 PM
- Check-out: 12:00 PM

Esto asegura uniformidad en la operación y simplifica la gestión de entradas y salidas.

### KYC Asistido por Inteligencia Artificial

Se implementó un proceso de validación de identidad mediante captura de documento y fotografía del usuario.

El sistema puede operar en:

- Modo simulado.
- Modo integrado con OpenAI GPT-4o Vision.

La validación forma parte del flujo de seguridad de la plataforma.

### Sistema Omnicanal de Notificaciones

La plataforma incorpora un sistema de alertas mediante:

- Notificaciones dentro de la aplicación.
- Correos electrónicos vía SMTP.

Las notificaciones son enviadas para eventos clave como:

- Registro de usuarios.
- Validación KYC.
- Confirmación de reservas.
- Recordatorios de llegada y salida.

### Dashboard para Propietarios

Los propietarios disponen de un panel de control que centraliza:

- Total de inmuebles registrados.
- Reservas activas.
- Ingresos generados.
- Tasa de ocupación.
- Historial de actividad reciente.

### Exportación de Reportes

Se implementó generación de archivos Excel (.xlsx) mediante ClosedXML.

Los reportes pueden generarse para:

- Todo el portafolio de inmuebles.
- Un inmueble específico.

Incluyendo:

- Huésped.
- Correo electrónico.
- Fechas de reserva.
- Número de noches.
- Precio.
- Total pagado.

### Seguridad de Datos

Los documentos utilizados durante el proceso KYC son tratados de forma temporal y eliminados una vez finalizada la validación, reduciendo riesgos asociados al almacenamiento de información sensible.

### Infraestructura Contenerizada

La solución se distribuye mediante Docker y Docker Compose para facilitar:

- Instalación rápida.
- Reproducibilidad del entorno.
- Despliegue consistente entre equipos y ambientes.

---

## Cumplimiento de Requerimientos

| Requerimiento | Estado |
|--------------|---------|
| Core desarrollado en .NET 10 | ✅ |
| Prevención de Double Booking | ✅ |
| Check-in 2:00 PM / Check-out 12:00 PM | ✅ |
| Catálogo público sin autenticación | ✅ |
| Filtros por ubicación y fechas | ✅ |
| Autenticación diferida | ✅ |
| Sistema de favoritos (Wishlist) | ✅ |
| Validación KYC con IA | ✅ |
| Dashboard para propietarios | ✅ |
| Reportes Excel (.xlsx) | ✅ |
| Notificaciones In-App | ✅ |
| Notificaciones por correo | ✅ |
| Gestión de inmuebles | ✅ |
| Docker y Docker Compose | ✅ |
| PostgreSQL + Entity Framework Core | ✅ |
| ASP.NET Core Identity | ✅ |
| Arquitectura MVC | ✅ |