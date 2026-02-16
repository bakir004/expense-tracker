# Expense Tracker API

This repository contains the source code for the Expense Tracker API, a RESTful web service built with ASP.NET Core. 
The API allows users to manage their expenses, including creating, reading, updating, and deleting expense records.

### Quickstart guide

This application is built using ASP.NET Core and can be run locally or in a Docker container.

#### Using `docker` locally

This application has been developed and tested using `docker` version 29.1.3.
To run the application using `docker` (`docker compose`), follow these steps:

1. Navigate to the project root directory in your terminal.
2. Run 
```bash
docker compose -f docker/docker-compose.yml up --build
```
3. You can gracefully stop the application with
```bash
docker compose -f docker/docker-compose.prod.yml down
```

You can change the parameters in the `docker-compose.yml` file to customize the database connection settings.
If you do, make sure to update the connection string in `appsettings.Development.json` to match the new settings.
All development settings in `appsettings.Development.json` will overwrite whatever is set in the `appsettings.json` file,
which you can think of as a "staging" settings file.

#### Running Locally
To run the application locally, you will need to have the .NET SDK installed on your machine. 
You can download it from the official [.NET website](https://dotnet.microsoft.com/download).

Check if you have the .NET SDK installed by running the following command in your terminal:

```bash
dotnet --version
```
If you see a version number, you have the .NET SDK installed. If not, please install it before proceeding.

Once you have the .NET SDK installed, you can run the application locally by following these steps:
1. Navigate to the project root directory in your terminal.
2. Run the following commands:

```bash
dotnet build
dotnet run --project src/ExpenseTracker.Api/ExpenseTracker.Api.csproj
```

Running the app locally using `dotnet` will require you to have a PostgreSQL database running.
There is a script prepared for easily starting a local database using `docker`:

```bash
chmod +x ./scripts/start-db.sh

./scripts/start-db.sh
```

In development mode, the API will attempt to connect to a PostgreSQL database at `localhost:5432` with the following credentials, which can be found in `appsettings.Development.json`:
- Host: `localhost`
- Username: `postgres`
- Password: `postgres`
- Database: `expense-tracker-db`
- Port: `5432`

The database on startup needs to be empty. Running the API will automatically apply migrations and create tables in the database.
Finally, it will seed it with some dummy data.

You can run the test suite using the following command from the root directory:

```bash
dotnet test
```

#### Using `docker` in production
To run the application in production using `docker`, you can use the `docker-compose.prod.yml` file,
which is configured to use the `.env` file for settings. `Program.cs` will try to pull environment variables
to form the connection string, and in case it fails, it has default fallbacks (except DB_PASSWORD which is required).

*Note: the `.env` file must be in the same location as the `docker-compose.prod.yml` file. If you like your `.env` file in the root of the project, then I suggest using symlinks to create a reference in the `/docker` directory that points to the root `.env`.*

For this, the allocated .yml file is `docker-compose.prod.yml` which relies on the image of API being already built.
Build the image with the following command (keep in mind that the `docker-compose.prod.yml` uses my Docker Hub username
so if you wish to include your own changes, dont forget to change the official image name):

```bash
docker build -t expense-tracker-api:latest -f docker/Dockerfile .
```

Then, after configuring your own image of the API you can start the application with:

```bash
docker compose -f docker/docker-compose.prod.yml up --build
```

If you wish to change database credentials, make sure to completely clean the docker cache, since even
after removing volumes it persists. Do:

```bash
# Step 1: Stop containers and remove volumes
docker compose -f docker/docker-compose.prod.yml down -v

# Step 2: Double-check volume is gone (optional)
docker volume ls | grep postgres

# Step 3: Start fresh
docker compose -f docker/docker-compose.prod.yml up -d
```

You can try to access the API at `http://localhost:5000` and the Swagger UI at `http://localhost:5000/swagger` to explore the endpoints.
Verify that the API is running correctly and can connect to the database by hitting the `/api/v1/health` endpoint.

Keep in mind that the API has 10 attempts to reconnect to the database in case of connection failure, meaning the API wont respond to anything until the database is up.

See the complete documentation at [ExpenseTracker API Documentation](docs/ARCHITECTURE.md) for detailed information on all endpoints, request/response formats, and usage examples.
