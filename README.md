# Eventrian

[![Build and Test](https://github.com/KenHSo/Eventrian/actions/workflows/build-and-test.yml/badge.svg)](https://github.com/KenHSo/Eventrian/actions/workflows/build-and-test.yml)

**Eventrian** is a modern web application for discovering and purchasing tickets to events.  
It offers a simple and responsive experience for users, with support for both internally created events and public events retrieved from external sources.

## Project Goals

Eventrian is a full-stack .NET project designed to explore real-world development patterns, secure architecture, and modern deployment workflows.

### Demo Experience

The application will include a demo mode that lets visitors explore different user roles:

- **Demo Admin**: Access a dashboard for managing events and users  
- **Demo User**: Browse events and simulate ticket purchases
- **Demo Chat**: Real-time chat between user and admin, powered by AI to simulate live support conversations  

All demo data will be temporary and reset between sessions to ensure system integrity.


### Features

- Browse and search events from internal and external sources  
- Purchase tickets through a streamlined user flow  
- Role-based access for users and admins  
- Admin dashboard for event and user management  
- Real-time updates and communication using SignalR   
- Integration with external APIs (e.g., event data and AI-powered chat)
- CI/CD pipeline for automated deployment  
- API security: rate limiting, response caching, CORS, input validation  


## Technologies

### Frontend
- Blazor WebAssembly

### Backend
- ASP.NET Core Web API  
- Entity Framework Core  
- SQL Server

### Shared
- Class library for DTOs and models

### Infrastructure
- Swagger/OpenAPI for documentation  
- Secure environment configuration using `.env` files and secrets 
- Docker + containerized deployment  
- Hosting: Azure, AWS, Linode, or similar  
- CI/CD via GitHub Actions

## Architecture
- Feature-based folder structure  
- Clean separation of concerns across API, Client, and Shared projects  
- Modular design for maintainability and scalability