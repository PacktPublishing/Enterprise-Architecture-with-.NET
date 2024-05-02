# DemoEditor sample information system

This is the sample application that comes with the book `Enterprise Architecture with .NET`, published by Packt. It is a system composed with several services that can be run together using Docker Compose. Some of the services are existing images, some are provided as custom, .NET-based, API-compatible, services.

## Context

**DemoEditor** is a sample company that is supposed to edit books. As such, it needs a data referential services for books, which is in `books-controller`, another one for authors, which is in `authors-controller`, and a portal application to exploit these data providers and add some business process. This application is a Blazor Web Assembly Single-Page Application, provided in the `portal-gui` folder. A mobile application based on MAUI is also provided for reference, but we will only show the installation of the web platform. The rest of the dependencies will be provided in the `docker-compose.yml` file.

## Caveat

The book is definitely not about programming. In fact, it is quite the contrary, as the method exposed in it insists on the importance of the right definition of services and API contracts, the respect of separation of responsibility and the use of norms and standards for interface, stating that those are more important than the code implementation itself. You will thus find lots of C# bits of code that are sub-optimal, or even quick-and-dirty versions of a method. **This is done on purpose**, both to stress the importance of the code structure rather than its execution details, and to give the readers opportunities to verify that adjusting implementations indeed does not impact the architecture in itself, owing to the right cutting of services, in alignment to the business functions.

This is of course not to say that I am against code improvements and I will be more than happy to receive any pull requests and possibly integrate them as examples of better implementations that are made easy by a solid initial architecture.

## Versioning

The sample application follows versions that hopefully make it easier to read the book chapters and associate them with different steps in the construction of the information system:
- Branch [v0.1](https://github.com/PacktPublishing/Enterprise-Architecture-with-.NET/tree/v0.1/DemoEditor) correspond to a very simple form of the application, with only the two APIs working and a basic portal, both without any authentication mechanism in order to ease use as a demo of the data referential services and overall comprehension of the concepts.
- Branch [main](https://github.com/PacktPublishing/Enterprise-Architecture-with-.NET/DemoEditor) is the most up-to-date version of the application, with maximum content, including applications from all chapters of the book (and thus highest level of complexity for a full installation).

## Prerequisites

Docker has been used in order to reduce as much as possible the required tooling. Since all images are available online (even the custom ones created for the sample information system, available at `https://hub.docker.com/repositories/demoeditor`), all you need to work with the application is Docker (see https://docs.docker.com/engine/install/ for installation instructions). The images versions follow the branches of the code.

If you want to debug the application, make some changes to it in order to follow the instructions from the book, then you will also need .NET 8.0 SDK (https://dotnet.microsoft.com/download), Visual Studio Code (https://code.visualstudio.com/download), and Git (https://git-scm.com/book/en/v2/Getting-Started-Installing-Git). Postman (https://www.postman.com/) will also be used as an option to quickly inject data.

## Installation

Even if everything is installed in the local machine, it makes it much easier to use aliases, so you will need to edit your `hosts` file (`/etc/hosts` for Linux, `C:\Windows\System32\drivers\etc\hosts` for Windows) and add the following lines:

```
127.0.0.1 iam
127.0.0.1 portal
127.0.0.1 authors
127.0.0.1 books
```

In order to avoid as much network conflicts as possible, the main application is exposed on port 88 instead of the default port 80. No HTTPS is used, again with the objective of simplifying as much as possible the deployment of the sample information system.

If you use the command line to retrieve the necessary files (you may also download the corresponding compressed package from the Github interface), you will need to use the following operations:

```
git clone https://github.com/PacktPublishing/Enterprise-Architecture-with-.NET
git checkout v0.1
```

Running the application is then as simple as launching Docker Compose on the right file:

```
cd DemoEditor
docker compose up -d
```

## Injecting some data

The files [DemoEditor.postman_collection.json](resources/DemoEditor.postman_collection.json) and [DemoEditor.postman_environment.json](resources/DemoEditor.postman_environment.json) can be imported into Postman to respectively create a collection of API calls and an environment, both called `DemoEditor`. You will find in the collection some operations to create sample data:

![](images/APIPostman.png)

Using the `DemoEditor` is needed to point correctly to the API implementations:

![](images/EnvPostman.png)

The commands use the variables of the environment in order for you to quickly adapt to your own setting. I recommend you run at least the `Create author` and `Create book` operations:

![](images/CreateBook.png)

## Running the application

Going to `http://portal:88` should provide the following interface (if an error occurs, check the 88 port is not already used in another process):

![](images/Welcome.png)

Clicking on the `Books` menu brings you to the list of books in the dedicated service:

![](images/Books.png)

From there, you can reach the author's edition form:

![](images/AuthorSave.png)

It is also possible to access the authors from the dedicated list (`Authors` menu):

![](images/Authors.png)

This interface allows you to edit authors with the auto-patch method (see the book for more information):

![](images/AuthorAutoPatch.png)

And that's about all you can do with this first, very limited, version. But its architecture is done in such a way that it is going to be easy for the system to follow business-led evolutions (this is the main subject of the book).