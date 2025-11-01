# Eventrian

[![Build and Test](https://github.com/KenHSo/Eventrian/actions/workflows/build-and-test.yml/badge.svg)](https://github.com/KenHSo/Eventrian/actions/workflows/build-and-test.yml)

**Learning project (2025)**  
Focused exploration of authentication and secure session handling in .NET.  
Implements short-lived access tokens, refresh tokens, and token rotation using ASP.NET Identity and JWT, with automated tests and a clean architectural structure.

---

## Purpose
Eventrian was created as a personal learning project to gain hands-on experience with building and testing secure authentication systems in ASP.NET Core.  
While originally intended as a full event management platform, the focus shifted entirely to implementing and hardening the authentication subsystem.

---

## Implemented Features
- ASP.NET Identity with custom user model  
- JWT authentication with short-lived access and refresh tokens  
- Token rotation for enhanced session security  
- Multi-session handling across browser tabs with user-specific logout broadcast  
- Role-based authorization (roles: Admin, Customer; policies: AdminOnly, CustomerOnly)  
- Unit and integration tests for authentication and token flows  
- Automated build and test pipeline with GitHub Actions  
- Clean Architecture separation between API, Application, and Infrastructure layers

---

## Technologies
- **Backend / Auth / Data:** ASP.NET Core Web API, Identity, JWT, EF Core, SQL Server (LocalDB)  
- **Testing / Infrastructure / Architecture:** xUnit, WebApplicationFactory, GitHub Actions, Clean Architecture

---

## Learning Focus
Through Eventrian, I explored:
- Implementing secure authentication and token lifecycle management in .NET  
- Integrating Identity and refresh token rotation  
- Managing user sessions across multiple browser tabs  
- Structuring and testing authentication flows  
- Applying Clean Architecture principles for maintainability  
- Using continuous integration to ensure build and test consistency
