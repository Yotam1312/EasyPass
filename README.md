# EasyPass

A simple, secure, and accessible password manager built with .NET.

## About

EasyPass is a lightweight password manager designed to make password storage effortless — especially for older users who value simplicity over complexity.

**The goal:** Help people securely store and manage their passwords without confusion or technical barriers.

Built as a cross-platform app using .NET MAUI with a .NET 8 Web API backend (hosted on Render), EasyPass combines a clean UI with secure authentication, reliable data management, and modern deployment using Docker containers.

## Project Status

**~95% Complete** — Ready for release!

- Backend fully functional and Dockerized
- Android app tested with live API on Render
- AES-256 encryption for stored passwords
- Complete CRUD operations with live connection
- Dependency Injection architecture
- Comprehensive error handling with retry logic
- Security features (clipboard auto-clear, session expiration)

## Key Features

| Feature | Description |
|---------|-------------|
| **Secure Authentication** | PIN-based login with BCrypt hashing |
| **AES-256 Encryption** | All passwords encrypted before storage |
| **JWT Tokens** | Secure API communication |
| **Full CRUD** | Add, edit, delete, search passwords |
| **Password Generator** | Built-in cryptographically secure generator |
| **Clipboard Security** | Auto-clears after 30 seconds |
| **Session Management** | Auto-logout on token expiration |
| **Error Handling** | User-friendly messages with retry options |
| **Password Visibility** | Show/hide toggle for each password |
| **Accessible Design** | Large buttons, readable text, simple navigation |

## Architecture

```
EasyPass/
├── EasyPass.API/     # .NET 8 Web API + EF Core + PostgreSQL + Docker
│   ├── Controllers/  # Auth, Passwords, Utils endpoints
│   ├── Services/     # JWT, Encryption, Password Generation
│   └── Models/       # User, PasswordEntry, DTOs
│
└── EasyPass.App/     # .NET MAUI Cross-Platform App
    ├── Views/        # LoginPage, RegisterPage, PasswordsPage
    ├── Services/     # PasswordService, AuthenticationService
    └── Models/       # PasswordEntry
```

**Design Patterns Used:**
- Dependency Injection (services registered in MauiProgram.cs)
- Service Layer (PasswordService for API calls)
- DelegatingHandler (AuthenticationHandler for JWT injection)

## Tech Stack

| Category | Technology |
|----------|------------|
| **Language** | C# |
| **Mobile Framework** | .NET MAUI 8.0 |
| **Backend Framework** | ASP.NET Core 8.0 |
| **ORM** | Entity Framework Core |
| **Database** | SQLServer (Production), SQLite (Development) |
| **Authentication** | JWT Tokens + BCrypt |
| **Encryption** | AES-256 CBC with SHA-256 key derivation |
| **Deployment** | Docker + Render |
| **IDE** | Visual Studio 2022 |

## Security Implementation

| Layer | Protection |
|-------|------------|
| **PIN Storage** | BCrypt with salt (cost factor 12) |
| **Password Storage** | AES-256 CBC with random IV per entry |
| **API Communication** | JWT Bearer tokens (1-hour expiry) |
| **Mobile Storage** | SecureStorage (Keychain/Keystore) |
| **Clipboard** | Auto-clear after 30 seconds |
| **Session** | Auto-logout on 401 response |
| **Transport** | HTTPS enforced, SSL database connection |

## API Endpoints

| Method | Endpoint | Auth | Description |
|--------|----------|------|-------------|
| POST | `/api/auth/register` | No | Register new user |
| POST | `/api/auth/login` | No | Login, returns JWT |
| GET | `/api/passwords` | JWT | List all passwords |
| POST | `/api/passwords` | JWT | Create password |
| PUT | `/api/passwords/{id}` | JWT | Update password |
| DELETE | `/api/passwords/{id}` | JWT | Delete password |
| GET | `/api/passwords/search?service=` | JWT | Search passwords |
| GET | `/api/utils/generate-password` | No | Generate strong password |
| GET | `/health` | No | API health check |

## Screenshots

*Coming soon*

## Future Enhancements

- Biometric login (fingerprint / Face ID)
- Offline mode with local caching
- iOS version
- Theme customization

## Motivation

Password managers are often over-engineered for non-technical users. EasyPass aims to deliver the same security with a much simpler experience.

The idea came after seeing how my elderly family members struggled to use traditional password managers. I wanted to create a version that feels simple, familiar, and friendly — without compromising on security or modern cloud reliability.

## License

This project was created as a Computer Science student project.
