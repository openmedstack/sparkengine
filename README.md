# Spark Engine

Spark is an open-source FHIR server developed in C#, initially built by Firely. Further development 
and maintenance is now done by Incendi.

Spark implements a major part of the FHIR specification and has been used and tested during several
HL7 WGM Connectathons.

**DISCLAIMER: The web projects Spark.Web and Spark are meant as reference implementations and should never be used out of the box in a production environment without adding as a minimum security features.**

## Get Started

There are two ways to get started with Spark. Either by using the NuGet packages and following the Quickstart Tutorial, or by using the Docker Images.

### NuGet Packages

Read the [Quickstart Tutorial](docs/Quickstart.md) on how to set up your own FHIR Server using the NuGet Packages. There is also an example project that accompanies the Quickstart Tutorial which you can find here: https://github.com/incendilabs/spark-example

### Docker Images

Set up the Spark FHIR server by using the Docker Images. Make sure you have installed [Docker](https://docs.docker.com/install/). On Linux you will need to install [Docker Compose](https://docs.docker.com/compose/install/) as well. After installing Docker you could run Spark server by running one of the following commands, found below, for your preferred FHIR Version. Remember to replace the single quotes with double quotes on Windows. The Spark FHIR Server will be available after startup at `http://localhost:5555`.

```shell
curl 'https://raw.githubusercontent.com/FirelyTeam/spark/r4/master/.docker/docker-compose.example.yml' > docker-compose.yml
docker-compose up
```

## Versions

### R4

Source code can be found in the branch **r4/master**. This is the version of Spark running at https://spark.incendi.no
FHIR Endpoint: https://spark.incendi.no/fhir

## Contributing

If you want to contribute, see our [guidelines](https://github.com/furore-fhir/spark/wiki/Contributing)

### Git branching strategy

Our strategy for git branching:

Branch from the `r4/master` branch which contains the R4 FHIR version, unless the feature or bug fix is considered for a specific version of FHIR then branch from the relevant branch which at this point is `stu3/master`.

See [GitHub flow](https://guides.github.com/introduction/flow/) for more information.
