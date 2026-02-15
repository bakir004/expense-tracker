# Expense Tracker API

This repository contains the source code for the Expense Tracker API, a RESTful web service built with ASP.NET Core. 
The API allows users to manage their expenses, including creating, reading, updating, and deleting expense records.

### Quickstart guide

This application is built using ASP.NET Core and can be run locally or in a Docker container.

#### Using `docker`

This application has been developed and tested using `docker` version 29.1.3.
To run the application using `docker` (`docker compose`), follow these steps:

1. Navigate to the project root directory in your terminal.
2. Run 
```bash
docker compose -f docker/docker-compose.prod.yml up
```
3. You can gracefully stop the application with
```bash
docker compose -f docker/docker-compose.prod.yml down
```

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
2. Run the following command to restore dependencies and run the application:

```bash
dotnet run --project src/ExpenseTracker.Api/ExpenseTracker.Api.csproj
```
