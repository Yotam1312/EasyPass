EasyPass 🔐

A simple, secure, and accessible password manager built with .NET.
## About

 EasyPass is a lightweight password manager designed to make password storage effortless  especially for older users who value simplicity over complexity.

The goal : help people securely store and manage their passwords without confusion or technical barriers.

Built as a cross-platform app using .NET MAUI with a .NET 9 Web API backend (hosted on Render) and a PostgreSQL cloud database,
EasyPass combines a clean UI with secure authentication, reliable data management, and modern deployment using Docker containers.
## Project Status 🚀

* The project is now fully live and deployed.
* All core functionality (registration, login with PIN, password management, and CRUD operations) is working and connected to a real cloud-based PostgreSQL database.
* The API runs as a Dockerized container on Render, ensuring scalability and consistent performance across environments.

🧱 Planned Features

🔒 Full AES-256 encryption for stored passwords

👆 Login via fingerprint or facial recognition

🎨 Complete UI/UX redesign for improved accessibility

📱 Android .apk build and future iOS support

## Key Features
🧍‍♂️ User registration & login with secure PIN authentication

🔐 Cloud-based password storage using PostgreSQL

🔁 JWT-based communication between the app and API

🧱 Entity Framework Core + PostgreSQL for robust ORM and data persistence

🔧 Add / edit / delete / search passwords easily

💡 Built-in strong password generator

🧓 Accessible design – large buttons, readable text, and minimal menus

🐳 Docker-based API deployment on Render
## Architecture

EasyPass/

├── EasyPass.API/ # Backend – .NET 9 Web API + EF Core + PostgreSQL + Docker

└── EasyPass.App/ # Frontend – .NET MAUI (cross-platform)

* The API handles authentication, data persistence, and JWT token management.

* The MAUI app provides a user-friendly interface for managing credentials across platforms.
## Tech Stack

Languages: C#

Frameworks: .NET MAUI, ASP.NET Core 9.0, Entity Framework Core

Database: PostgreSQL (Render Cloud)

Auth: JWT Tokens

Deployment: Docker + Render

Tools: Visual Studio 2022, Git, GitHub


## Security Notes
* User PINs are hashed and salted before storage.

* All communication between the app and API is secured using JWT tokens.

* The database connection is fully SSL-encrypted.

* AES-256 encryption for password data is planned for the next release.
## Motivation

Password managers are often over-engineered for nontechnical users.
EasyPass aims to deliver the same security with a much simpler experience.
It’s designed for older users who just want a clear, comfortable way to manage their digital lives.

The idea came after seeing how my elderly family members struggled to use traditional password managers.
I wanted to create a version that feels simple, familiar, and friendly without compromising on security or modern cloud reliability.
