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


## Docker

### Construir e iniciar los servicios

```bash
docker compose up --build
```
---

## 📂 Estructura del Proyecto

```text
RentalHub/
├── Areas/
│   └── Owner/
│       ├── Controllers/
│       │   ├── DashboardController.cs
│       │   ├── KycController.cs
│       │   ├── PropertyManagementController.cs
│       │   └── ReportController.cs
│       │
│       └── Views/
│           ├── Dashboard/
│           ├── Kyc/
│           ├── PropertyManagement/
│           ├── Report/
│           ├── _ViewImports.cshtml
│           └── _ViewStart.cshtml
│
├── Constants/
│   ├── ReservationStatus.cs
│   └── Roles.cs
│
├── Controllers/
│   ├── AccountController.cs
│   ├── HomeController.cs
│   ├── KycController.cs
│   ├── NotificationController.cs
│   ├── ReservationController.cs
│   └── WishlistController.cs
│
├── Data/
│   └── ApplicationDbContext.cs
│
├── Migrations/
│
├── Models/
│   ├── ApplicationUser.cs
│   ├── KycVerification.cs
│   ├── Notification.cs
│   ├── Payment.cs
│   ├── Property.cs
│   ├── PropertyImage.cs
│   ├── Reservation.cs
│   └── Wishlist.cs
│
├── Services/
│   ├── Interfaces/
│   │   ├── IEmailService.cs
│   │   └── IKycService.cs
│   │
│   └── Implementations/
│       ├── EmailService.cs
│       ├── KycService.cs
│       └── ReminderService.cs
│
├── ViewModels/
│   ├── DashboardViewModel.cs
│   ├── LoginViewModel.cs
│   └── RegisterViewModel.cs
│
├── Views/
│   ├── Account/
│   ├── Home/
│   ├── Kyc/
│   ├── Notification/
│   ├── Reservation/
│   ├── Shared/
│   ├── Wishlist/
│   ├── _ViewImports.cshtml
│   └── _ViewStart.cshtml
│
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

Se implementó un proceso de validación de identidad que solicita:

- Documento de identidad.
- Selfie sosteniendo el documento.

El sistema puede operar en:

- Modo simulado (sin API Key).
- Modo integrado con OpenAI GPT-4o Vision.

La verificación es obligatoria antes de realizar reservas o publicar inmuebles.

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

Esto facilita la toma de decisiones basada en datos.

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
