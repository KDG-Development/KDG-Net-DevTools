# DEVELOPMENT USE **ONLY** !!

## Overview
This suite of development tools is designed to assist with development efforts when interacting with the KDG Boilerplate.

## Setup

1. Clone this repository to your local boilerplate inside `./DevTools`
> Take extra caution to ensure these files aren't commited to version control!

## User Manager

#### Create a development user

1. Include your connection string at `DevTools/appsettings.json'
> This probably shouldn't ever be a production environment
1. Run `dotnet run --project=UserManager/UserManager.csproj`