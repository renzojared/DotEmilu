# DotEmilu EF Core

A simple, easy-to-use .NET library designed for EF Core interceptors, auditable entities.

## Features

- Supports **CREATE**, **UPDATE**, **DELETE** tracking.
- Provides properties for base entities.
- Provides a contract to implement http user context information.

## How to Use

Follow these simple steps to get started:

1. **Define Your Database Entities**  
   Create your database entities as needed for your application.

   ```csharp
   //To track inherit from BaseAuditableEntity
   public class Person : BaseAuditableEntity
   {
       public string? Name { get; set; }
       public string? Surname { get; set; }
   }
   ```

2. **Implement Context User Information**  
   Create an implementation to retrieve user context information (e.g., user ID, roles).

   ```csharp
   //Feel free to inject your necessary dependencies to provide user information.
   public class ContextUser : IContextUser
   {
       public Guid Id => Guid.NewGuid();
   }
   ```

3. **Register the Interceptor**  
   Add your custom interceptor to the dependency injection container in your application.

   ```csharp
   builder.Services
       .AddDotEmiluInterceptors<ContextUser>();
   ```