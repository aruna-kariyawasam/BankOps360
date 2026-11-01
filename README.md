\# BankOps360



\### Banking Operations, Payment Integration \& Transaction Monitoring Platform



BankOps360 is an independent .NET portfolio project that demonstrates a banking operations platform built with \*\*C#, ASP.NET Core, Entity Framework Core and Microsoft SQL Server\*\*.



The system combines customer and account management, transaction processing, double-entry ledger recording, external payment integration, AML-style transaction monitoring and operational reporting.



It also includes a simulated clearing-bank service supporting REST/JSON and SOAP/XML communication.



\---



\## Architecture



```mermaid

flowchart TB

&#x20;   User\[Bank Employee / API Client]



&#x20;   subgraph Application\[BankOps.Web - ASP.NET Core]

&#x20;       MVC\[MVC Web Interface]

&#x20;       API\[REST Web API]

&#x20;       Auth\[Identity and JWT Authentication]

&#x20;       TX\[Banking Transaction Service]

&#x20;       PAY\[Payment Processing Service]

&#x20;       AML\[AML Monitoring Service]

&#x20;       REP\[Reporting Service]

&#x20;   end



&#x20;   DB\[(SQL Server)]



&#x20;   subgraph Clearing\[BankOps.ClearingSimulator]

&#x20;       REST\[REST / JSON Endpoint]

&#x20;       SOAP\[SOAP 1.1 / XML Endpoint]

&#x20;   end



&#x20;   User --> MVC

&#x20;   User --> API

&#x20;   API --> Auth

&#x20;   MVC --> TX

&#x20;   API --> TX

&#x20;   API --> PAY

&#x20;   API --> AML

&#x20;   API --> REP

&#x20;   TX --> DB

&#x20;   PAY --> DB

&#x20;   AML --> DB

&#x20;   REP --> DB

&#x20;   PAY --> REST

&#x20;   PAY --> SOAP

```



\## Main Features



\### 1. Customer Management

\- Create and manage customer records.

\- Search and retrieve customer information.

\- Update and deactivate customer records.

\- MVC interface and REST API endpoints.



\### 2. Bank Account Management

\- Savings and Current account creation.

\- Account number generation.

\- LKR account currency support.

\- Account information retrieval.



\### 3. Banking Transaction Engine

\- Deposits.

\- Withdrawals.

\- Internal account transfers.

\- Double-entry ledger recording.

\- Database transaction handling.

\- Idempotency protection against duplicate submissions.

\- Optimistic concurrency handling.

\- Transaction audit logging.



\### 4. Payment Integration



An external clearing-bank simulator demonstrates payment communication using two protocols:



\- REST API with JSON.

\- SOAP 1.1 with XML.



The payment workflow includes:

\- Payment submission and response processing.

\- API-key authentication.

\- Idempotency handling.

\- Payment acceptance and rejection.

\- Compensating ledger entries following a rejection.

\- Handling of uncertain processing outcomes.



\### 5. Transaction Monitoring



The AML-style monitoring module includes illustrative detection rules:



\- Individual high-value transactions.

\- Frequent transactions within a short time window.

\- Cumulative transaction activity within 24 hours.



Alert management includes risk scores, review statuses and compliance-officer workflows.



\### 6. Operational Reporting

\- Transaction summaries.

\- Ledger reconciliation.

\- SQL Server stored procedures.

\- Parameterized ADO.NET reporting queries.



\## Security Features



\- ASP.NET Core Identity.

\- Role-based access control.

\- JWT bearer authentication.

\- Secure password policy.

\- Account lockout.

\- API rate limiting.

\- Clearing-service API-key authentication.

\- Security response headers.

\- Development-only Swagger interface.

\- Sensitive credentials stored using .NET User Secrets during development.



\## Technology Stack



| Area | Technology |

|---|---|

| Language | C# |

| Framework | .NET 8 |

| Web application | ASP.NET Core MVC |

| API | ASP.NET Core Web API |

| ORM | Entity Framework Core 8 |

| Database | Microsoft SQL Server / LocalDB |

| Authentication | ASP.NET Core Identity and JWT |

| Integration | REST, JSON, SOAP 1.1, XML |

| Reporting | T-SQL and ADO.NET |

| Testing | xUnit v3 |

| API documentation | Swagger / OpenAPI |

| Source control | Git and GitHub |



\## Solution Structure



```text

BankOps360/

│

├── BankOps.Web/

│   ├── Controllers/

│   │   ├── Api/

│   │   └── CustomersController.cs

│   ├── Data/

│   │   ├── Migrations/

│   │   └── ApplicationDbContext.cs

│   ├── Models/

│   ├── Services/

│   ├── Views/

│   └── Database/

│       └── Scripts/

│

├── BankOps.ClearingSimulator/

│   └── Program.cs

│

├── BankOps.Tests/

│   └── BankingTransactionServiceTests.cs

│

├── .gitignore

└── BankOps360.sln

```



\## Getting Started



\### Prerequisites



\- Visual Studio 2022.

\- .NET 8 SDK.

\- SQL Server LocalDB.

\- Git.



\### 1. Clone the repository



```powershell

git clone https://github.com/aruna-kariyawasam/BankOps360.git



cd BankOps360

```



\### 2. Restore dependencies



```powershell

dotnet restore

```



\### 3. Configure development secrets



The application uses .NET User Secrets for sensitive configuration.



Configure the following values for `BankOps.Web`:



| Configuration key | Purpose |

|---|---|

| `Jwt:SigningKey` | Base64 JWT signing key (minimum 256 bits) |

| `SeedAdmin:Email` | Development administrator email |

| `SeedAdmin:Password` | Development administrator password |

| `ClearingBank:ApiKey` | API key used by the clearing client |



Generate a JWT signing key:



```powershell

$jwtKey = \[Convert]::ToBase64String(

&#x20;   \[Security.Cryptography.RandomNumberGenerator]::GetBytes(32)

)



dotnet user-secrets set "Jwt:SigningKey" $jwtKey --project .\\BankOps.Web



Remove-Variable jwtKey

```



Set the administrator credentials:



```powershell

dotnet user-secrets set "SeedAdmin:Email" "developer@example.local" --project .\\BankOps.Web



dotnet user-secrets set "SeedAdmin:Password" "REPLACE\_WITH\_A\_STRONG\_PASSWORD" --project .\\BankOps.Web

```



Generate one shared API key for both clearing components:



```powershell

$clearingKey = \[Convert]::ToBase64String(

&#x20;   \[Security.Cryptography.RandomNumberGenerator]::GetBytes(32)

)



dotnet user-secrets set "ClearingBank:ApiKey" $clearingKey --project .\\BankOps.Web



dotnet user-secrets set "ClearingSimulator:ApiKey" $clearingKey --project .\\BankOps.ClearingSimulator



Remove-Variable clearingKey

```



\### 4. Apply database migrations



Using Visual Studio Package Manager Console:



```powershell

Update-Database -Project BankOps.Web -StartupProject BankOps.Web

```



The default development connection uses SQL Server LocalDB.



\### 5. Start the clearing simulator



Open a terminal:



```powershell

dotnet run --project .\\BankOps.ClearingSimulator --no-launch-profile

```



The simulator listens on:



```text

http://localhost:5081

```



Health endpoint:



```text

http://localhost:5081/health

```



\### 6. Start the main web application



Open another terminal:



```powershell

dotnet run --project .\\BankOps.Web --launch-profile https

```



Application:



```text

https://localhost:7227

```



Swagger:



```text

https://localhost:7227/swagger

```



The default clearing protocol is SOAP. The protocol can be selected through the `ClearingBank:Protocol` configuration setting (`Soap` or `Rest`).



\## Automated Testing



Run:



```powershell

dotnet test .\\BankOps.Tests\\BankOps.Tests.csproj -c Release

```



The integration test suite covers:



1\. Deposit balance updates and balanced ledger entries.

2\. Duplicate deposit protection.

3\. Insufficient-funds withdrawal rejection.

4\. Successful withdrawal.

5\. Internal transfer debit and credit.

6\. Same-account transfer rejection.

7\. Idempotency-key reuse with a different transaction payload.



\*\*Latest recorded verification:\*\* Release build completed with zero warnings and zero errors; all 7 automated tests passed.



The NuGet vulnerability scan also reported no vulnerable packages in the solution at the time of verification.



\## Project Scope and Limitations



BankOps360 is a learning and portfolio demonstration, not a production banking platform.



\- The clearing service is a simulator, not a connection to a real bank or live CEFT/SLIPS network.

\- The simulator uses in-memory payment storage, which resets when it restarts.

\- AML detection rules and thresholds are illustrative and are not official regulatory requirements.

\- LocalDB and User Secrets are intended for development.

\- Production deployment would require additional infrastructure, operational controls, security assessment and regulatory review.



This project currently targets .NET 8. A supported runtime upgrade is recommended before long-term deployment.



\## Author



\*\*Aruna Kariyawasam\*\*



GitHub: https://github.com/aruna-kariyawasam



\---



\*Developed as an independent portfolio project to demonstrate practical .NET software engineering, database development, financial transaction processing and system integration concepts.\*

