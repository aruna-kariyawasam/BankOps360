 <p align="center">
  <img src="https://capsule-render.vercel.app/api?type=transparent&fontColor=58A6FF&fontSize=58&height=110&width=1000&text=BankOps360&desc=Banking%20Operations%20%7C%20Payment%20Integration%20%7C%20Transaction%20Monitoring&descAlignY=78&descSize=15" alt="BankOps360" />
</p>

<p align="center">
  <img src="https://readme-typing-svg.demolab.com?font=Fira+Code&weight=600&size=20&pause=1200&color=58A6FF&center=true&vCenter=true&width=950&height=45&lines=A+Banking+Operations+Platform;Built+with+C%23+%7C+ASP.NET+Core+%7C+SQL+Server;REST+%26+SOAP+Payment+Integration;Transaction+Processing+%26+AML+Monitoring" alt="Project headlines" />
</p>

<p align="center">
  <a href="https://github.com/aruna-kariyawasam/BankOps360">
    <img src="https://img.shields.io/badge/Project-BankOps360-58A6FF?style=for-the-badge&logo=github&logoColor=white" alt="BankOps360" />
  </a>
  <img src="https://img.shields.io/badge/.NET-8.0-512BD4?style=for-the-badge&logo=dotnet&logoColor=white" alt=".NET 8" />
  <img src="https://img.shields.io/badge/C%23-239120?style=for-the-badge&logo=csharp&logoColor=white" alt="C Sharp" />
  <img src="https://img.shields.io/badge/SQL%20Server-CC2927?style=for-the-badge&logo=microsoftsqlserver&logoColor=white" alt="SQL Server" />
  <img src="https://img.shields.io/badge/License-Portfolio%20Project-blue?style=for-the-badge" alt="Portfolio Project" />
</p>

---

### 🚀 About the Project

**BankOps360** is an independent banking operations and payment integration platform developed using C#, ASP.NET Core, Entity Framework Core, and Microsoft SQL Server.

The project demonstrates how a banking-oriented software system can manage customers, bank accounts, financial transactions, external payment communication, transaction monitoring, and operational reporting through an integrated application architecture.

The platform consists of two main applications and an automated testing project:

- **BankOps.Web** – ASP.NET Core MVC application and REST API.
- **BankOps.ClearingSimulator** – External clearing-bank simulator supporting REST/JSON and SOAP/XML.
- **BankOps.Tests** – Automated integration tests for banking transaction operations.

### ✨ Key Features

#### 🏦 Banking Operations
- Customer registration and management.
- Savings and Current account creation.
- Account information retrieval.
- Deposit and withdrawal operations.
- Internal account transfers.
- Transaction history and audit logging.

#### 💳 Transaction Processing Engine
- Double-entry bookkeeping and ledger recording.
- Atomic database transaction handling.
- Idempotency protection against duplicate requests.
- Optimistic concurrency handling.
- Financial balance validation.
- Transaction reference generation.

#### 🔗 Payment Integration

<p align="center">
  <img src="https://img.shields.io/badge/REST-JSON-009688?style=for-the-badge&logo=fastapi&logoColor=white" alt="REST JSON" />
  <img src="https://img.shields.io/badge/SOAP-XML-4479A1?style=for-the-badge&logo=xml&logoColor=white" alt="SOAP XML" />
</p>

The clearing simulator demonstrates communication between banking systems using two integration protocols:

- REST API with JSON payloads.
- SOAP 1.1 with XML messages.
- API-key-based service authentication.
- Duplicate payment request protection.
- Accepted and rejected payment responses.
- Compensating ledger entries following payment rejection.
- Handling of uncertain payment-processing outcomes.

#### 🛡️ AML-Style Transaction Monitoring
- High-value transaction detection.
- Frequent transaction activity monitoring.
- Cumulative transaction amount monitoring.
- Risk-score generation.
- Alert review and status management.
- Administrator and Compliance Officer workflows.

#### 📊 Operational Reporting
- Transaction summary reports.
- General ledger reconciliation.
- SQL Server stored procedures.
- Parameterized ADO.NET database queries.

### 🏗️ System Architecture

<p align="center">
  <img src="https://img.shields.io/badge/ASP.NET%20Core-MVC%20%26%20Web%20API-512BD4?style=flat-square&logo=dotnet&logoColor=white" alt="ASP.NET Core" />
  <img src="https://img.shields.io/badge/EF%20Core-ORM-512BD4?style=flat-square&logo=dotnet&logoColor=white" alt="EF Core" />
  <img src="https://img.shields.io/badge/SQL%20Server-Database-CC2927?style=flat-square&logo=microsoftsqlserver&logoColor=white" alt="SQL Server" />
</p>

```mermaid
flowchart TB
    U["Bank Employee / API Client"]

    subgraph WEB["BankOps.Web - ASP.NET Core"]
        MVC["MVC Web Interface"]
        API["REST Web API"]
        AUTH["Identity + JWT Authentication"]
        TX["Banking Transaction Service"]
        PAY["Payment Processing Service"]
        AML["AML Monitoring Service"]
        REP["Reporting Service"]
    end

    DB[("Microsoft SQL Server")]

    subgraph CLEARING["BankOps.ClearingSimulator"]
        REST["REST / JSON Endpoint"]
        SOAP["SOAP 1.1 / XML Endpoint"]
    end

    U --> MVC
    U --> API
    API --> AUTH
    MVC --> TX
    API --> TX
    API --> PAY
    API --> AML
    API --> REP

    TX --> DB
    PAY --> DB
    AML --> DB
    REP --> DB

    PAY --> REST
    PAY --> SOAP
```

### 🛠️ Technology Stack

<p align="left">
  <img src="https://img.shields.io/badge/C%23-239120?style=for-the-badge&logo=csharp&logoColor=white" alt="C Sharp" />
  <img src="https://img.shields.io/badge/.NET%208-512BD4?style=for-the-badge&logo=dotnet&logoColor=white" alt=".NET 8" />
  <img src="https://img.shields.io/badge/ASP.NET%20Core-512BD4?style=for-the-badge&logo=dotnet&logoColor=white" alt="ASP.NET Core" />
  <img src="https://img.shields.io/badge/Entity%20Framework%20Core-512BD4?style=for-the-badge&logo=dotnet&logoColor=white" alt="EF Core" />
  <img src="https://img.shields.io/badge/SQL%20Server-CC2927?style=for-the-badge&logo=microsoftsqlserver&logoColor=white" alt="SQL Server" />
  <img src="https://img.shields.io/badge/ADO.NET-4479A1?style=for-the-badge&logo=databricks&logoColor=white" alt="ADO.NET" />
  <img src="https://img.shields.io/badge/REST%20API-009688?style=for-the-badge&logo=fastapi&logoColor=white" alt="REST API" />
  <img src="https://img.shields.io/badge/SOAP-4479A1?style=for-the-badge&logo=xml&logoColor=white" alt="SOAP" />
  <img src="https://img.shields.io/badge/JSON-000000?style=for-the-badge&logo=json&logoColor=white" alt="JSON" />
  <img src="https://img.shields.io/badge/XML-FF6600?style=for-the-badge&logo=xml&logoColor=white" alt="XML" />
  <img src="https://img.shields.io/badge/xUnit-5E2B97?style=for-the-badge&logo=xunit&logoColor=white" alt="xUnit" />
  <img src="https://img.shields.io/badge/Swagger-85EA2D?style=for-the-badge&logo=swagger&logoColor=black" alt="Swagger" />
  <img src="https://img.shields.io/badge/Git-F05032?style=for-the-badge&logo=git&logoColor=white" alt="Git" />
  <img src="https://img.shields.io/badge/GitHub-181717?style=for-the-badge&logo=github&logoColor=white" alt="GitHub" />
  <img src="https://img.shields.io/badge/Visual%20Studio-5C2D91?style=for-the-badge&logo=visualstudio&logoColor=white" alt="Visual Studio" />
</p>

### 🔐 Application Security

- ASP.NET Core Identity authentication.
- JWT bearer authentication.
- Role-based authorization.
- Password complexity requirements.
- Account lockout policies.
- API rate limiting.
- Clearing-service API-key authentication.
- Security response headers.
- Sensitive development credentials managed with .NET User Secrets.
- Production-only security configuration.

### 🧪 Automated Testing

The project includes an xUnit integration test suite using isolated SQL Server LocalDB databases.

| Test scenario | Expected behaviour |
|---|---|
| Deposit | Account balance increases and ledger entries balance |
| Duplicate deposit | Duplicate request does not post twice |
| Insufficient funds | Withdrawal is rejected |
| Successful withdrawal | Account balance decreases |
| Internal transfer | Source is debited and destination credited |
| Same-account transfer | Invalid transfer is rejected |
| Idempotency conflict | Reusing a key with a different payload is rejected |

<p align="center">
  <img src="https://img.shields.io/badge/Automated%20Tests-7%2F7%20Passed-2EA44F?style=for-the-badge&logo=githubactions&logoColor=white" alt="Seven out of seven tests passed" />
  <img src="https://img.shields.io/badge/Release%20Build-0%20Warnings%20%7C%200%20Errors-2EA44F?style=for-the-badge" alt="Release build passed" />
  <img src="https://img.shields.io/badge/NuGet%20Vulnerability%20Scan-No%20Findings-2EA44F?style=for-the-badge" alt="No vulnerable NuGet packages reported" />
</p>

### 📁 Solution Structure

```text
BankOps360/
│
├── BankOps.Web/
│   ├── Areas/
│   ├── Controllers/
│   │   └── Api/
│   ├── Data/
│   │   └── Migrations/
│   ├── Models/
│   │   └── Banking/
│   ├── Services/
│   ├── Views/
│   ├── Database/
│   │   └── Scripts/
│   └── wwwroot/
│
├── BankOps.ClearingSimulator/
│   ├── Properties/
│   └── Program.cs
│
├── BankOps.Tests/
│   └── BankingTransactionServiceTests.cs
│
├── .gitignore
└── BankOps360.sln
```

### ⚙️ Getting Started

**Prerequisites**

- Visual Studio 2022.
- .NET 8 SDK.
- SQL Server LocalDB.
- Git.

**1. Clone the repository**

```powershell
git clone https://github.com/aruna-kariyawasam/BankOps360.git

cd BankOps360
```

**2. Restore dependencies**

```powershell
dotnet restore
```

**3. Configure development secrets**

Use .NET User Secrets to configure the JWT signing key, development administrator credentials, and matching clearing-service API keys.

The required configuration key names can be checked in `Program.cs`, `IdentitySeeder.cs`, and `ClearingBankClient.cs`.

Do not commit passwords, API keys, signing keys or real connection credentials.

**4. Apply database migrations**

Using Visual Studio Package Manager Console:

```powershell
Update-Database -Project BankOps.Web -StartupProject BankOps.Web
```

**5. Start the clearing simulator**

```powershell
dotnet run --project .\BankOps.ClearingSimulator --no-launch-profile
```

The simulator listens on:

```text
http://localhost:5081
```

**6. Start the main web application**

Open another terminal:

```powershell
dotnet run --project .\BankOps.Web
```

Use the application URL printed by ASP.NET Core in the terminal.

### 🧪 Run Tests

```powershell
dotnet test .\BankOps.Tests\BankOps.Tests.csproj -c Release
```

### ⚠️ Project Scope & Limitations

BankOps360 is an independent software engineering portfolio project and a banking-system simulation.

- The clearing service is a simulator, not a real bank connection or live CEFT/SLIPS integration.
- Payment records in the simulator use in-memory storage and reset when it restarts.
- AML detection thresholds and rules are illustrative, not official regulatory requirements or GoAML certification.
- SQL Server LocalDB and User Secrets are intended for development.
- Production deployment would require further infrastructure, testing, security assessment, operational controls and regulatory review.
- The project currently targets .NET 8 and requires a supported-runtime upgrade before long-term deployment.

### 🔗 Project Repository

<p align="center">
  <a href="https://github.com/aruna-kariyawasam/BankOps360">
    <img src="https://img.shields.io/badge/Explore%20Source%20Code-BankOps360-181717?style=for-the-badge&logo=github&logoColor=white" alt="Explore BankOps360 on GitHub" />
  </a>
</p>

### 👨‍💻 Developer

<p align="center">
  <a href="https://github.com/aruna-kariyawasam">
    <img src="https://img.shields.io/badge/GitHub-aruna--kariyawasam-181717?style=for-the-badge&logo=github&logoColor=white" alt="GitHub Profile" />
  </a>
</p>

<p align="center">
  <i>Developed as an independent portfolio project to demonstrate .NET software engineering, financial transaction processing, database development and system integration.</i>
</p>

---
