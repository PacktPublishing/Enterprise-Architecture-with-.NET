# DemoEditor sample information system

This is the sample application that comes with the book `Enterprise Architecture with .NET`, published by Packt. It is a system composed with several services that can be run together using Docker Compose. Some of the services are existing images, some are provided as custom, .NET-based, API-compatible, services.

## Context

**DemoEditor** is a sample company that is supposed to edit books. As such, it needs a data referential services for books, which is in `books-controller`, another one for authors, which is in `authors-controller`, and a portal application to exploit these data providers and add some business process. This application is a Blazor Web Assembly Single-Page Application, provided in the `portal-gui` folder. A mobile application based on MAUI is also provided for reference, but we will only show the installation of the web platform. The rest of the dependencies will be provided in the `docker-compose.yml` file.

## Caveat

The book is definitely not about programming. In fact, it is quite the contrary, as the method exposed in it insists on the importance of the right definition of services and API contracts, the respect of separation of responsibility and the use of norms and standards for interface, stating that those are more important than the code implementation itself. You will thus find lots of C# bits of code that are sub-optimal, or even quick-and-dirty versions of a method. **This is done on purpose**, both to stress the importance of the code structure rather than its execution details, and to give the readers opportunities to verify that adjusting implementations indeed does not impact the architecture in itself, owing to the right cutting of services, in alignment to the business functions.

This is of course not to say that I am against code improvements and I will be more than happy to receive any pull requests and possibly integrate them as examples of better implementations that are made easy by a solid initial architecture.

## Versioning

**It is extremely important that you choose the right version if you want to use the present code to check how things presented in the book actually work.** Versions before 1.0 are there to show the progressive building of the information system; refer to them if you want to see simple code for a particular use. Version 1.0 is targeted as the version that correspond to the state of the system at the end of the book, with all parts working together; run this version if you want to have almost everything explained in the book running at once, but still without any sophistication. Versions following 1.0 will be added to make the code cleaner, add some options that have not been shown or discussed in the book, etc. Since they will be more sophisticated and configurable, they might become a bit more difficult to read and associate with the book, so only refer to them if you want to know about possible improvements from what the book concentrated on (which, again, is definitely not the technical preciseness, but the right alignment of the technology on the business).

The sample application follows versions that hopefully make it easier to read the book chapters and associate them with different steps in the construction of the information system:
- Branch [v0.1](https://github.com/PacktPublishing/Enterprise-Architecture-with-.NET/tree/v0.1/DemoEditor) corresponds to a **very simple form of the application**, with only the two APIs working and a basic portal, both **without any authentication mechanism** in order to ease use as a demo of the data referential services and overall comprehension of the concepts. This is the version you should use if you want to start as simple as possible and work your way up by yourself, following the steps indicating in the book to grow the sample information system.
- Branch [v0.2](https://github.com/PacktPublishing/Enterprise-Architecture-with-.NET/tree/v0.2/DemoEditor) adds the **authentication and authorization management** to the application (both frontend and backend) using a Keycloak IAM server. It also adds the batch import of data, using a Docker volume.
- Branch [v0.3](https://github.com/PacktPublishing/Enterprise-Architecture-with-.NET/tree/v0.3/DemoEditor) shows the implementation of a business process for the book creation. Rather than simply adding an entity, this process progressively enhances the structure of the book. This version also adds a webhook mechanism that updates the locally-duplicated attributes of the main author of a book when this author is modified.
- Branch [v0.4](https://github.com/PacktPublishing/Enterprise-Architecture-with-.NET/tree/v0.4/DemoEditor) adds two services. The first one is based on MailHog and sends invites to prospect authors for a new book. The second one is a custom Middle Office so that prospect users can indicate whether they accept of reject this invite.
- Branch [v0.5](https://github.com/PacktPublishing/Enterprise-Architecture-with-.NET/tree/v0.5/DemoEditor) introduces the notification service and points the existing services to this new tool. As a side-effect, this version also brings the feature of auto-provisionning of users on the corresponding server.
- Branch [v0.6](https://github.com/PacktPublishing/Enterprise-Architecture-with-.NET/tree/v0.6/DemoEditor) goes further in the business process choreography by adding a webhook callback when an author is chosen, which in turns generates a contract in the Electronic Document Management service. Since this exchange of contract needs some hard delivery robustness, we will introduce a Message-Oriented Middleware at this step. **Warning: the IAM service switches from port 8080 to 8088 in this version (see explanation below).**
- Branch [v0.7](https://github.com/PacktPublishing/Enterprise-Architecture-with-.NET/tree/v0.7/DemoEditor) introduces orchestration with n8n, using its editor to react to the creation of an authoring contract. A very simple reporting mechanism is also added in this version, in the form of a PowerBI Desktop report.
- Branch [v0.8](https://github.com/PacktPublishing/Enterprise-Architecture-with-.NET/tree/v0.8/DemoEditor) adds an Open Policy Agent service to externalize the business rules that determine which channel should be used for a notification from the context (identity of the destination, time and day, priority of the message, available channels, etc.).
- Branch [v1.0](https://github.com/PacktPublishing/Enterprise-Architecture-with-.NET/tree/v1.0/DemoEditor) completes the information system with a monitoring feature, in order to check that everything works fine (as the complete system is quite complex and needs for you to follow carefully the installation manual below). This version also cleans up a few things in the code, add some content on the documentation to ease manipulation, while still avoiding any sophistication that would make it harder to read (for example, URLs of services are hardcoded). **This is the version you should use if you want to use the code to better understand what is explained in the chapters of the book.**
- Branch [main](https://github.com/PacktPublishing/Enterprise-Architecture-with-.NET/tree/main/DemoEditor) is the most up-to-date version of the application, with **maximum content, including applications from all chapters** of the book (and thus highest level of complexity for a full installation) **but also additional content that is outside the scope of the book**. Use this version if you want to follow the future works of the author on this sample information system.

## Prerequisites

Docker has been used in order to reduce as much as possible the required tooling. Since all images are available online (even the custom ones created for the sample information system, available at `https://hub.docker.com/repositories/demoeditor`), all you need to work with the application is Docker (see https://docs.docker.com/engine/install/ for installation instructions). The images versions follow the branches of the code.

If you want to debug the application, make some changes to it in order to follow the instructions from the book, then you will also need .NET 8.0 SDK (https://dotnet.microsoft.com/download), Visual Studio Code (https://code.visualstudio.com/download), and Git (https://git-scm.com/book/en/v2/Getting-Started-Installing-Git). Postman (https://www.postman.com/) will also be used as an option to quickly inject data. Power BI (https://www.microsoft.com/en/power-platform/products/power-bi/) can be used to read a provided report that directly calls the data referential services through their API endpoints.

## Installation

### Setting up the hosts

Even if everything is installed in the local machine, it makes it much easier to use aliases, so you will need to edit your `hosts` file (`/etc/hosts` for Linux, `C:\Windows\System32\drivers\etc\hosts` for Windows) and add the following lines:

```
127.0.0.1 iam
127.0.0.1 portal
127.0.0.1 authors
127.0.0.1 books
127.0.0.1 mail
127.0.0.1 middleoffice
127.0.0.1 users
127.0.0.1 edm
127.0.0.1 mom
127.0.0.1 brms
127.0.0.1 status
```

### Running the services

In order to avoid as much network conflicts as possible, the main application is exposed on port 88 instead of the default port 80. No HTTPS is used, again with the objective of simplifying as much as possible the deployment of the sample information system.

If you use the command line to retrieve the necessary files (you may also download the corresponding compressed package from the Github interface), you will need to use the following operations:

```
git clone https://github.com/PacktPublishing/Enterprise-Architecture-with-.NET
git checkout [code of the version you want to work on]
```

Running the application is then as simple as launching Docker Compose on the right file:

```
cd DemoEditor
docker compose up -d
```

### Defining IAM (import mode)

You can perform this step manually if you prefer (see next section), but an export is provided in the `resources` folder in order to make it easier. To import IAM settings, connect to the IAM console (http://iam:8088) with the credentials found in the Docker Compose file, then got to the `create realm` command in the top menu, browse the `demoeditor-realm-export.json` and import it.

![](images/realmsetup.gif)

### Defining IAM (manual mode)

The Identity and Authorization Management server (Apache Keycloak, in our case) has been activated in the Docker Compose service for this version, and must be configured:
1. Connect to http://iam:8088/admin/master/console/, using the credentials defined in the `docker-compose.yml` file.
2. Create a realm called `demoeditor`.
3. Add 3 realm roles, named `editor`, `director` and `author`.
4. Create a client with `portal` as its id, and use default in the `Capability config` tab of the wizzard.
5. In the following step, add the login-callback, logout-callback and web origins for localhost, but also ports 81 and 82.

The content should be like follows (the ports are the one inside Docker Compose, not the exposed ones):

![](images/ClientSettings.png)

6. Click the `portal-dedicated` link in the `Client scopes` tab.
7. Add the predefined mapper called `realm roles`.
8. In this new mapper, activate the `Add to ID token` setting.

### Adding users in the IAM

10. Add a `francesca` user.
11. Give it credentials to be able to use it (you can suppress their temporary nature in order not to have to change the password at first connection).
12. Assign this account the `director` role.

![](images/usercreation.gif)

Note that a production use would need to harden a bit the IAM, and in particular add a database to store settings in a persistant manner. With the current way of creating them, they will simply be stored in the volume automatically created by Docker Compose, and thus lost if you operate a `docker compose down` operation.

### Injecting some data

The files [DemoEditor.postman_collection.json](resources/DemoEditor.postman_collection.json) and [DemoEditor.postman_environment.json](resources/DemoEditor.postman_environment.json) can be imported into Postman to respectively create a collection of API calls and an environment, both called `DemoEditor`. You will find in the collection some operations to create sample data:

![](images/APIPostman.png)

Using the `DemoEditor` is needed to point correctly to the API implementations:

![](images/EnvPostman.png)

The commands use the variables of the environment in order for you to quickly adapt to your own setting. I recommend you run at least the `Create author` and `Create book` operations:

![](images/CreateBook.png)

Since we have activated authentication and authorization in this version, it is necessary to edit the `Authorization` parameters of the `DemoEditor` collection (all levels below inherit from it), and obtain an OAuth 2.0 token, with such a configuration to retrieve it from Keycloak (check the book for all details):

![](images/PostmanToken.png)

### Batch import of data

Sending data by hand is not very effective, so we will take advantage of the `import` volume declared in the Docker Compose file and use the following command to load the Excel workbook provided in the folder called `resources` (note that all commands are provided Linux-style; if you are using Windows, please operate them through WSL):

```
docker run --rm -v ./resources/DemoEditor-BooksCatalog.xlsx:/tmp/DemoEditor-BooksCatalog.xlsx -v demoeditor_import:/tmp/data ubuntu cp /tmp/DemoEditor-BooksCatalog.xlsx /tmp/data
```

Once this is done, you may use the following Postman operation to integrate the content from the Excel workbook:

![](images/Import.png)

A GUI to inspect content of the MongoDB database is also provided in this version, with credentials visible in the `docker-compose.yml` file to connect to such an interface:

![](images/DBGUI.png)

### Defining the middle office behaviour

In order for the middle office to work properly, it is necessary to create a template definition, by using the Postman collection, and in particular the `Declare template` operation under folder `MiddleOffice`:

![](images/TemplateMiddleOffice.png)

### External Electronic Document Management system (option)

Starting in version 0.6, we also install an Alfresco system. Since this uses quite a lot of resources and its use is anecdotical in the sample (basically just to show how to correctly externalize document management), this is left to a separate Docker Compose installation. The file used can be obtained at https://github.com/Alfresco/acs-deployment/blob/master/docker-compose/community-docker-compose.yml and a copy is provided at the root of the present repository. Note that the code has been written in such a way that the absence of the EDM does not normally cause any bug, and does not prevent the correct execution of the business process. Only the steps associated with files will be skipped or ignored.

As this complex Docker Compose defines port 8080 as its principal exposition and uses it in most of its services, internally as well as externally, it has been decided to switch the Apache Keycloak IAM exposition port to 8088 to avoid conflict. The documentation has been changed accordingly, and the code remains as such, except for the accesses from the Single Page Application that have been modified. The Postman collection file has also been updated. The server calls remain the same because they use internal ports, all services inside Docker Compose being on shared networks. Note that you will have to recreate your users when changing the IAM port, and that notifications to the old users will be lost (it may be better to clear the whole `users` collection in the database).

To run the Alfresco set of services, use the following command:

```
docker compose -f alfresco-community-docker-compose.yml up -d
```

**Do not try to mix the two Docker Compose files, as they both use a MOM and a conflict can arise on port 5672 even if the `mom` service has been reexposed externally on 5673. Also, when two Docker Compose files are executed in the same directory, there is a message stating that some orphans services are present when running them: this is normal, as each Docker Compose application considers the other one as services with the same folder name but not appearing in their own list. If this bothers you, you can put the EDM Docker Compose file in another directory.**

Once everything is ready (it can take a few minutes at first initialization), you can connect to the interface Share from port 8080 on the 127.0.0.1 local host (or with the `edm` alias). Login is achieved with the default `admin` / `admin` credentiels:

![](images/LoginAlfresco.png)

This console is where you will be able to check that the sample files are indeed created in the EDM folders. This is also where you can add metadata schemas, and generally speaking realize administration operations on the CMIS-compatible server. The very first thing to do is to create a receiving folder under the root of the repository you just logged in. To do so, go to `My files`, and click on `Create...` then `Folder`:

![](images/CreateFolder.png)

For the examples below to work, this folder needs to be named precisely `DemoEditor`:

![](images/FolderName.png)

After that, we need to execute a second, a bit more complex, administration operation. If you go to `Admin tools` and `Model Manager`, you will see the following interface that will allow you to import a metadata schema:

![](images/AlfrescoModels.png)

A `DemoEditor.zip` Custom Module Manager is provided in the `resources` folder for your convenience. It contains a few metadata definition for the type of document that we will use in this sample, namely an authoring contract. Once imported, you need to activate the model:

![](images/ActivateModel.png)

Clicking on the `DemoEditor` link will show you the content:

![](images/ModuleContent.png)

And going inside the `de:AuthorContrat` will list the associated metadata for this type of document:

![](images/MetadataList.png)

The `Action` menu allows for edition of these metadata fields. For example, on the `de:contractType`, we could specify a constraint that only some values are authorized, like this:

![](images/ConstraintContractType.png)

In the same menu, you will find the `Layout designer` that enables the creation of a graphical structure to display the metadata. Here is an example with two columns:

![](images/LayoutDesigner.png)

**Please note that this approach with metadata is a bit different from the one shown in the book. For simplicity reason, the section about EDM was reduced in the chapter about external dependencies, and limited the interop to passing URLs of documents (CMIS 1.0). What is even better, because it reduces coupling and makes it as low as possible, is to only depend on this contracts of metadata, by systematically using queries based on them. This way, a client does not call an absolute URL for a document, but instead sends a CMIS query like `{{CMISURL}}?cmisselector=query&succinct=true&q=select * from de:AuthorContract where de:authorId='jpgou'` to the EDM server (whatever implementation of CMIS 1.1 it is) and retrieves the associated documents.**

The EDM system should now be ready for use in the Information System business processes managing electronic documents (contracts, in this case), but a quick test on Postman, using the dedicated `Documents` folder, will allow you to check everything is in place. You may need to adjust the `CMISURL` environment setting, using the URL provided in the Alfresco parameters, under `CMIS 1.1 Browser Binding URL`:

![](images/AlfrescoParams.png)

### Adding orchestration

The server called n8n is used to externalize process management. In order for the two requested workflows to be added, you need to enter the console of n8n by browsing to `http://localhost:5678/workflows` and clicking on the `Add workflow` button.

![](images/ImportChoreography.png)

Selecting the file called `Contract_Generated.json` and provided in the `resources` subfolder will create a workflow that listens to the RabbitMQ MOM and executes some operations. Doing so with the second file called `Nightly_import_of_legacy_books_list.json` will create another process that will be fired every day at 00:00:

![](images/Trigger.png)

Do not forget to activate the two workflows if they have not been upon creation:

![](images/Workflows.png)

Since the export does not include the password, for obvious security reasons, it is normal that, once you import the workflows, they appear with the following errors:

![](images/MissingCredentials.png)

You will need to adjust the tasks requiring authentication for the whole system to work. For example, on the import task of the second workflow, a Keycloak user with `director` role must be provided using the OAuth2 mode:

![](images/Credentials.png)

As indicated in the form, you will need to add `http://localhost:5678/rest/oauth2-credential/callback` as a valid redirection URL in the Keycloak customization of the `portal` client if you want to reuse it for this purpose. Be careful that, if you run in local mode and HTTP, the redirection mechanism may force you to externalize the IAM in its own Docker Compose file, just like as been done for the EDM.

Note that you may receive an error message upon activation of the n8n workflow, stating that the queue `ContractGenerated` does not exist. This may be due to the fact that the `contracts` service is not up and running. It has been passed with a `restart: on-failure` option to prevent this problem. Also, you may have to send a first request for contract by chosing a main author for a new book to make this queue visible. Since all is inside robust queues, nothing is lost anyway and, once all the setup is correct, you will find the messages that had not been treated before, but not lost.

### Notification rules

In order for the notification channels externalized decision engine to work, you will need to send the customized `NotificationPolicy.rego` file (to be found under the `resources` folder) to the Open Policy Agent, by using the Postman operation captured below:

![](images/CustomizedNotificationPolicy.png)

### PowerBI reporting

The file `DemoEditor.pbix` is provided in the `Resources` folder. You will have to adjust the authentication setup to access the data from your API endpoints if you have changed the default API keys for the data referential services. To do so, click on the `Data` / `Transform data` menu of PowerBI Desktop, which brings you to the PowerQuery window:

![](images/PowerQuery.png)

When there, click on `Advanced editor` and modify the key value in the text definition of the data query:

![](images/ChangeAPIKey.png)

You may have to repeat the same operation for the other data query, in our case the one called `Authors`.

### Setting up the monitoring system

As a final step of this admittedly long installation procedure (but all those steps have been left for manual setup in order for you to get the grip of the whole system), we finally put the necessary parameters in the monitoring system that has been chosen (https://statping-ng.github.io/). This way, not only will the sample information system be under control, but this will help you check everything is OK before running the test scenario below.

First, navigate to `http://status/8183` and create the connexion as required. Then, go to the `Settings` menu in the dashboard, and click on `Import` command. Browse to the `statping.json` file provided in the `resources` folder and import it to the system. The public dashboard (at the root URL of the service) should like this:

![](images/Dashboard.png)

If you are still in the private dashboard, the interface will be more like the following:

![](images/PrivateDashboard.png)

Whichever dashboard you use, all services should be green, at least in the mandatory and important services groups. The optional services group contains for example the Alfresco EDM and the GUI for the database. The application can work without these services, even if, of course, the generated contracts will in this case not be persisted anywhere.

## Running the application

### Accessing the GUI

Going to `http://portal:88` should provide the following interface (if an error occurs, check the 88 port is not already used in another process):

![](images/Welcome.png)

Most menus are invisible, until you click on `Log in` and connect using the `francesca` user that has been created above, and who have most accesses, owing to her `director` role:

![](images/Login.png)

Clicking on the `Books` menu brings you to the list of books in the dedicated service:

![](images/Books.png)

From there, you can reach the author's edition form:

![](images/AuthorSave.png)

It is also possible to access the authors from the dedicated list (`Authors` menu):

![](images/Authors.png)

This interface allows you to edit authors with the auto-patch method (see the book for more information):

![](images/AuthorAutoPatch.png)

If you log out, create a user with a simple `editor` role, which provides fewer accesses (or even `author`, which has almost no rights), then log in again using this new user, you will notice some menus do not appear. In addition, trying to enter the URL directly will not help and you will be rejected anyway:

![](images/Unauthorized.png)

And that's about it for this second version of the sample application. The evolution to a secured system has been realized without touching any of the business function, but that sounds logical. The real test of the architecture will be when adding some business-related features, which will be the subjects of the following versions (see other branches of the repository).

### Starting the book creation process

Still in the portal, under the `Books` menu, click on the `Start project for a new book`. This should bring you to a form where you will be able to progress step by step in a book creation. If you read the book, a GUI-based wizzard is one of the most common implementations of business processes. In this case, the BPM will be as follows:

![](images/BookCreationBPMN.png)

And the corresponding GUI is this form:

![](images/BookCreation.png)

When in second step, you should drag and drop some authors to send them an invite:

![](images/ProspectAuthorsSelection.png)

Once the choice is validated, the process integrated in the API will create a request in the middle office service and send an invite to the authors. The invite can be found in the mail server:

![](images/AuthorInvite.png)

The content of the mail is the form coming from the middle office:

![](images/MiddleOfficeRequest.png)

If you click on the first button, another mail will be sent explaining the author has accepted the proposal. There is no action attached to the second choice. Finally, the third choice will send a mail to the editors in order to ask them to call the author:

![](images/RequestForContact.png)

### Creating an authoring contract

Back to the portal, the next step is about entering the author that has been chosen by the editors once all the invitation messages have been received and, for some, responded to. The work of the editors to select which is the best person is something considered manual at this stage of the information system, but we could imagine that a decision process, with author responses evaluation, could be added in the future. It will be easy to add due to the design of the system, or even to replace the whole process with an external BPMN engine, since all interfaces have been standardized.

![](images/ChoosingAuthor.png)

As soon as the step is validated, the fact that there is a change in the author of the book triggers a message that we can see in the RabbitMQ management console:

![](images/RabbitMQ.png)

This message is received by the `contracts` service that generates a PDF and sends it to the EDM service, which is Alfresco in our case. After a few moments, you can notice that the document is available (and indexed) inside the EDM "Share" interface:

![](images/ContractAlfresco.png)

Clicking on the document shows a preview of the contract generated in PDF and the operations available in Alfresco:

![](images/DetailsContract.png)

Going down in the interface, on the right in the `Properties` tab, you will find the metadata associated to the contract:

![](images/MetadataContract.png)

During this time, a message was sent back on the MOM (the queue is called `ContractGenerated`) to produce a roundtrip (pattern "request and response" of the EIP). This time the service called `bpm` and based on the product n8n is used in order to react to this message, sending a message to the author explaining their contract is ready for signing, then creating a folder with the book code inside a OneDrive share:

![](images/Orchestration.png)

### Using reporting software

A `DemoEditor.pbix` file is provided under the `resources` folder. It is used as a sample of what one can do with Power BI connected to the two main API endpoints of the system, namely Authors and Books. **API keys should be treated as secrets but have been provided here in clear text in the `appsettings.json` file for ease of setup. Please change them and use orchestrator secrets or at least environment variables in your production systems.**

This file is already plugged to the API endpoints using the sample keys, as can be seen in the properties of the queries:

![](images/APIKeysPowerBI.png)

Opening it will show you a very simple reporting dashboard:

![](images/PowerBIDashboard.png)

## Notes

### Structure of the project

All projects for the sample Information System have been provided in a single repository, with all versions aligned to the branches of this repository. In a real production context, they would have been completely separated, the only link remaining being the `common` project, which only carries contracts-related objects. And even this is questionable if you want to reach complete version decoupling. It should at least be provided in versioned components, each service being responsible for using the right version of the entities.

As an additional safety in case of incompatibility with new versions (in the long term, adjusting to major versions can get difficult) or if the Docker images corresponding to the old versions of dependencies were to disappear, they have been saved on the `demoeditor` Docker Hub repository. In case they would need to get used instead of the official images, the names of the images in the `docker-compose.yml` file should be prefixed with `demoeditor/` if they are not already. The images in the end should be like this:

```
demoeditor/mongo:4.4
demoeditor/mongo-express:1.0.2
demoeditor/keycloak:23.0.7
demoeditor/mailhog:v1.0.1
demoeditor/rabbitmq:3.13-management
demoeditor/n8n:1.42.1
demoeditor/opa:0.64.1
demoeditor/statping-ng:v0.90.78
```

### Improvements on the project

As any information system, the DemoEditor sample will never be considered as completely done. Its use was mostly to serve as a support for the concepts explained in the associated book, and this is why it has remained as unsophisticated as possible in the [v1.0](https://github.com/PacktPublishing/Enterprise-Architecture-with-.NET/tree/v1.0/DemoEditor) version, which is the one in line with the print release of the book (May 2024). Still, the fact that no logs are present, that injection has not been used where it should have been, that models have not been shared (and are neither complete) and lots of other limitations remain a problem for a set of applications that is supposed to help readers learn about better coding. This is why, starting from version 1.0, additional changes will progressively be added to the application in order to show some possible improvements.

I would be more than happy to welcome pull requests from readers or anyone finding this repository and willing to improve upon the existing system. Please remember, though, that this application has to serve as a pedagogical support for a book about business / IT alignment and its goal is to provide code as simple as possible to make the associated concepts clearer.

Here is a non-exhaustive list of tasks and ideas:
- Correct all code marked with a `TODO`
- Add some traces for important operations
- Include a simple log-reading mechanism (Portainer?)
- Plug the traces to a centralized tracing system (Prometheus, for example, since it can receive data from statping)
- Centralize a library with all models
- Separate the DTO from the classes used for MongoDB persistance
- Inject class instances like notification utility, etc.
- Add some end-to-end tests on the portal
- Add unit tests for the most complicated units, like the notification channel rules in OPA
- Continue the process from the author's point of view, by exposing them the contract for review in a separate, simpler, EDM
- Extend this external EDM with a One-Time Token mechanism for the authors to view the contract
- Create a second implementation of BRMS using JBPM to compare ease of use with OPA
- Add a centralized index data server that register for changes from the data referential and exposes pre-agregated data
- Once more data and attributes are added, improve on the PowerBI report to generate something with more business value
- Use secrets instead of hard-coded keys (only if it does not harm the readability of the exercise)
- Test replacement of RabbitMQ by a Redis server