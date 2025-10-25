# Code-Explainer-Back-end
Back-end of Code Explainer project. AI for SE

## Table of content
- [Installation](#installation)
- [Usage](#usage)
- [Structure](#project-structure)
- [Environment](#environment)
- [License](#license)
## Installation

The project required [.NET](https://dotnet.microsoft.com/en-us/download/dotnet/9.0) SDK 9

**NOTE:** Install dotnet ef core (If already installed, skip this section)
```csharp
dotnet tool install --global dotnet-ef
```

## Usage

- Clone the repository
```bash
git clone https://github.com/kleqing/Code-Explainer-Back-end.git
```

- Build
```bash
dotnet build
```

- Create database from model (required dotnet ef core)
```bash
dotnet ef migrations add "Initial" --project CodeExplainer.BusinessObject  --startup-project CodeExplainer.WebApi --context ApplicationDbContext
dotnet ef database update --project CodeExplainer.BusinessObject  --startup-project CodeExplainer.WebApi --context ApplicationDbContext
```

*API Endpoint:* https://localhost:7077/swagger/index.html

## User Data Example in Database
```json
[
  {
    "UserId": "fbaad9a2-b90b-4d01-9c28-4916559868f3",
    "UserName": "kleqing",
    "Email": "nhan@gmail.com",
    "PasswordHash": "$2a$12$XwDGzci2f28o4TknNwYV.ehtYyCGYjCcleG/BshDlFoAK2ntgcELW", (@Test is the password)
    "EmailConfirmed": true,
    "UserRole": 1,
    "ProfilePictureUrl": "string",
    "RefreshToken": "",
    "RefreshTokenExpiryTime": "",
    "CreatedAt": "2025-10-31 21:32:01.896944",
    "UpdatedAt": "2025-10-31 21:32:01.896944"
  }
]
```



## Environment

- PostgreSQL (Database)
- Jetbrains Rider (Other IDE still work)
- TablePlus (SQL Management)

## License

This project is licensed under the [MIT License](https://github.com/kleqing/Code-Explainer-Back-end/blob/main/LICENSE)
