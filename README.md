# Chronofoil.Web

This is the API for Chronofoil that handles all online aspects of Chronofoil.
The plugin portion is [here](https://github.com/ProjectChronofoil/Chronofoil.Plugin).

## What is it?

This is the API code for Chronofoil services.
It handles:
- Authentication, registration via third-party providers
- Receiving, storing, and serving opcodes for censoring
- Retrieving information such as new ToS and the latest FAQ
- All Capture file handling on the server-side

## Running Chronofoil.Web

Chronofoil.Web is organized to be as seamless as possible to develop and deploy.

The docker-compose file in the root directory allows you to run a local instance of the 
Chronofoil API simply by running `docker compose up`. In order to get auth working, you
can either hardcode fields (not recommended), add another auth provider in the code,
or simply make a Discord application and set your Client ID and Client Secret in the
`appsettings.Development.json` file. You must recompile the plugin and change the endpoint 
in order to utilize a local or hosted instance of Chronofoil.