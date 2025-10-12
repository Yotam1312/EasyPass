# EasyPass 🔐  
*A simple, secure, and accessible password manager built with .NET.*

---

## About

**EasyPass** is a lightweight password manager designed to make password storage effortless — especially for **older users** who value simplicity over complexity.

The goal: **help people securely store and manage their passwords without confusion or technical barriers.**

Built as a cross-platform app using **.NET MAUI** with a **.NET Core Web API** backend, EasyPass combines a clean UI with secure authentication and database management.

---

## Project Status 🚧

This project is currently **in development**.  
The core functionality (API, login with PIN, password management, and CRUD operations) is already working,  
but several key features are still planned for future updates:

### 🧱 Planned Features
- 🧍‍♂️ User registration screen (currently only login is implemented)  
- 🔒 Full AES-256 encryption for stored passwords  
- 👆 Login via **fingerprint** or **facial recognition**  
- 🎨 Complete UI/UX redesign for improved accessibility  
- 📱 Android **.apk** build and future **iOS** support  

---

## Key Features (Implemented)

- 🔑 **PIN-based login** – simple and intuitive authentication  
- 🔒 **Secure password storage** using hashing and salting  
- 🔁 **JWT-based communication** between the app and API  
- 🧱 **Entity Framework + SQLite** for lightweight local data storage  
- 🔧 **Add / edit / delete / search** passwords easily  
- 💡 **Built-in strong password generator**  
- 🧓 **Accessible design** – large buttons, readable text, and minimal menus  

---

## Architecture

EasyPass/
├── EasyPass.API/ # Backend – .NET Core Web API + EF Core + SQLite
└── EasyPass.App/ # Frontend – .NET MAUI (cross-platform)

**The API handles authentication, data persistence, and JWT token management.  
**The MAUI app provides a user-friendly interface for managing credentials.

---

## Tech Stack

- **Languages:** C#  
- **Frameworks:** .NET MAUI, .NET Core, Entity Framework Core  
- **Database:** SQLite  
- **Auth:** JWT Tokens  
- **Tools:** Visual Studio 2022, Git, GitHub  

---

## Getting Started

1️. Clone the repository
```bash
git clone https://github.com/Yotam1312/EasyPass.git
cd EasyPass
```

2. Run the backend
```
cd EasyPass.API
dotnet run
```

3. Launch the MAUI app
Open the solution EasyPass.sln in Visual Studio and run EasyPass.App.

---
## Security Notes

User PINs are hashed and salted before storage.

JWT tokens secure all API requests.

AES-256 encryption planned for next version.
---
## *Motivation*

Password managers are often over-engineered for nontechnical users.  
**EasyPass** aims to deliver the same security  with a much simpler experience.  
It’s designed for **older users** who just want a clear, comfortable way to manage their digital lives.

The idea came after seeing how my elderly family members struggled to use traditional password managers.  
I wanted to create a version that feels simple, familiar, and friendly without compromising on security.

