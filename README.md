# StarEvents
StarEvents is a robust, production-ready **Event Management and Logistics Web Application** engineered using the **ASP.NET Core (MVC)** design pattern. Built upon clean, decoupled architecture layers, it empowers event management organizations to register organizers, publish dynamic event schedules, track vendor details, and manage multi-tier user authentications.

---

## 🚀 Architectural & System Features

* **Layered Clean Architecture:** Features a modular codebase separating presentation (`Views`, `Controllers`), persistent database layers (`Data`, `Migrations`), standalone Domain Schemas (`Models`), and reusable underlying business handlers (`Services`).
* **Role-Based Access Control (RBAC):** Integrated natively with **ASP.NET Core Identity** and EF Core Roles to seamlessly deliver custom views and restricted dashboard interactions across distinct roles (`Admin`, `Organizer`, `Client`).
* **Dynamic Event Lifecycle & Logic Engines:** Driven by a specialized, decoupled `StarEvents.Services` assembly handling custom server-side constraints and calculations during live event workflows.
* **EF Core Code-First Migrations:** Comprehensive, version-controlled state transitions (`InitialIdentity`, `AddEventFields`, `ChangeOrganizerIdToString`, and `SimplifyVenue`) map out optimized underlying SQL tables over time.
* **Polished Frontend Validation:** Dynamic forms with client-side real-time rendering leveraging Bootstrap 5 assets, customized theme overrides, and `jQuery Validation Unobtrusive`.

---

## 🛠️ Tech Stack & Dependencies

* **Backend Framework:** C# (#), .NET Core 8.0+ (ASP.NET Core MVC)
* **ORM / Database Engine:** Entity Framework Core, SQL Server / Microsoft Azure SQL
* **Security & Auth:** ASP.NET Core Identity Provider & Token/Cookie Schemas
* **Frontend Layer:** Razor Views (HTML5/CSS3), JavaScript (ES6), Bootstrap v5.x, jQuery, jQuery Validation

