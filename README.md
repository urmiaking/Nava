# Nava
This application is a web api for a music managing system called 'Nava' using both SQL and NoSQL databases.
## Technology Stack
- ASP.NET Core 5 Web API
- SQL Server
- MongoDB
## Architecture and used techniques
- Clean & Onion Architecture
- AutoFac (for DI)
- AutoMapper
- EF Core 5 (as SQL ORM - Code first approach)
- ```MongoDB.Driver``` (for querying MongoDB)
- Repository Pattern (for access data)
- ASP.NET Core Identity (for user management)
- OAuth2 (for authentication)
- Swashbuckle (Swagger)
- API Versioning
## How it's work?
On first run of the application, tables will be migrated into visual studio's SQL Server instance (of course you can change the connection string in ```appsettings.json``` file).
Then it will seed the database with an admin user. same thing will happen for mongoDB database. After data seeded into databases successfully, Swagger will generate __Open API__'s
specification in a json file and then it will show a UI consisting of controllers and API methods with their needed parameters and you can start trying them in your browser.
## Application Logic
This system consists of four main entities which is **User**, **Artist**, **Media** and **Album**. Users can be either admin user or casual user. Admin user has full control over
CRUD Opeerations of other entities. while casual user only can see artist, media and album's information and can modify it's own user account. So accessing api methods except login
and register methods requires authentication. Each user can follow any artist and like and visit any media. Album's can be belong to multiple artists and medias can only belong to
just an album.
