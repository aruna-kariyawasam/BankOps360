 <p align="center">
  <img src="https://capsule-render.vercel.app/api?type=transparent&fontColor=58A6FF&fontSize=58&height=100&width=1000&text=BankOps360&desc=Banking%20Operations%20%7C%20Payment%20Integration&descAlignY=78&descSize=16" alt="BankOps360" />
</p>

<p align="center">
  <img src="https://readme-typing-svg.demolab.com?font=Fira+Code&weight=600&size=19&pause=1200&color=58A6FF&center=true&vCenter=true&width=900&height=40&lines=ASP.NET+Core+%7C+C%23+%7C+SQL+Server;Banking+Transaction+Processing;REST+%26+SOAP+Payment+Integration;Transaction+Monitoring+%26+Reporting" alt="Project headlines" />
</p>

<p align="center">
  <img src="https://img.shields.io/badge/C%23-239120?style=for-the-badge&logo=csharp&logoColor=white" alt="C#" />
  <img src="https://img.shields.io/badge/.NET%208-512BD4?style=for-the-badge&logo=dotnet&logoColor=white" alt=".NET 8" />
  <img src="https://img.shields.io/badge/ASP.NET%20Core-512BD4?style=for-the-badge&logo=dotnet&logoColor=white" alt="ASP.NET Core" />
  <img src="https://img.shields.io/badge/SQL%20Server-CC2927?style=for-the-badge&logo=microsoftsqlserver&logoColor=white" alt="SQL Server" />
  <img src="https://img.shields.io/badge/EF%20Core-512BD4?style=for-the-badge&logo=dotnet&logoColor=white" alt="EF Core" />
</p>

---

### 🚀 About the Project

**BankOps360** is an independent .NET portfolio project demonstrating a banking operations platform built with C#, ASP.NET Core, Entity Framework Core and Microsoft SQL Server.

It brings together customer management, financial transactions, payment integration, transaction monitoring and operational reporting in one application.

### ✨ Key Features

- 🏦 **Banking Operations** – Customer management, account creation, deposits, withdrawals and internal transfers.
- 💳 **Transaction Engine** – Double-entry ledger, database transactions, idempotency and concurrency handling.
- 🔗 **Payment Integration** – REST/JSON and SOAP/XML clearing-bank simulator.
- 🛡️ **Transaction Monitoring** – Illustrative AML-style rules, risk scores and alert management.
- 📊 **Reporting** – Transaction summaries and ledger reconciliation using SQL Server stored procedures.
- 🔐 **Security** – ASP.NET Core Identity, JWT authentication, role-based authorization and API rate limiting.

### 🏗️ Architecture

```text
BankOps360
│
├── BankOps.Web
│   ├── ASP.NET Core MVC
│   ├── REST Web API
│   ├── Banking Services
│   ├── Authentication & Authorization
│   └── SQL Server / EF Core
│
├── BankOps.ClearingSimulator
│   ├── REST / JSON
│   └── SOAP / XML
│
└── BankOps.Tests
    └── xUnit Integration Tests
```

### 🛠️ Technology Stack

<p align="left">
  <img src="https://img.shields.io/badge/C%23-239120?style=for-the-badge&logo=csharp&logoColor=white" alt="C#" />
  <img src="https://img.shields.io/badge/ASP.NET%20Core-512BD4?style=for-the-badge&logo=dotnet&logoColor=white" alt="ASP.NET Core" />
  <img src="https://img.shields.io/badge/Entity%20Framework%20Core-512BD4?style=for-the-badge&logo=dotnet&logoColor=white" alt="EF Core" />
  <img src="https://img.shields.io/badge/SQL%20Server-CC2927?style=for-the-badge&logo=microsoftsqlserver&logoColor=white" alt="SQL Server" />
  <img src="https://img.shields.io/badge/REST%20API-009688?style=for-the-badge" alt="REST API" />
  <img src="https://img.shields.io/badge/SOAP-4479A1?style=for-the-badge" alt="SOAP" />
  <img src="https://img.shields.io/badge/xUnit-5E2B97?style=for-the-badge&logo=xunit&logoColor=white" alt="xUnit" />
  <img src="https://img.shields.io/badge/Swagger-85EA2D?style=for-the-badge&logo=swagger&logoColor=black" alt="Swagger" />
  <img src="https://img.shields.io/badge/Git- F05032?style=for-the-badge&logo=git&logoColor=white" alt="Git" />
</p>

### ⚙️ Getting Started

**Prerequisites:** Visual Studio 2022, .NET 8 SDK and SQL Server LocalDB.

```powershell
git clone https://github.com/aruna-kariyawasam/BankOps360.git
cd BankOps360
dotnet restore
dotnet build -c Release
dotnet test -c Release
```

Configure the required development credentials and API keys using .NET User Secrets, then apply the EF Core migrations:

```powershell
Update-Database -Project BankOps.Web -StartupProject BankOps.Web
```

Run `BankOps.ClearingSimulator` and `BankOps.Web` separately.

### ⚠️ Project Scope

This is an educational and portfolio demonstration, not a production banking system. The clearing service is simulated, and AML detection rules are illustrative rather than official regulatory requirements.

### 👨‍💻 Developer

**Aruna Kariyawasam**

<p align="center">
  <a href="https://github.com/aruna-kariyawasam">
    <img src="https://img.shields.io/badge/GitHub-aruna--kariyawasam-181717?style=for-the-badge&logo=github&logoColor=white" alt="GitHub Profile" />
  </a>
  <a href="https://github.com/aruna-kariyawasam/BankOps360">
    <img src="https://img.shields.io/badge/Repository-BankOps360-58A6FF?style=for-the-badge&logo=github&logoColor=white" alt="BankOps360 Repository" />
  </a>
</p>

---
