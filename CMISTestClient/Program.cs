﻿// This code crashes when trying to switch to CMIS 1.1 atom binding, as explained in the following link:
// https://stackoverflow.com/questions/37300149/error-property-cmtitle-doesnt-exsist-while-using-dotcmis-for-alfresco

using DotCMIS;
using DotCMIS.Client;
using DotCMIS.Client.Impl;

Dictionary<string, string> parameters = new Dictionary<string, string>();
parameters[SessionParameter.BindingType] = BindingType.AtomPub;
parameters[SessionParameter.AtomPubUrl] = "http://edm:8080/alfresco/api/-default-/public/cmis/versions/1.0/atom";
//parameters[SessionParameter.AtomPubUrl] = "http://edm:8080/alfresco/api/-default-/public/cmis/versions/1.1/atom";
parameters[SessionParameter.User] = "admin";
parameters[SessionParameter.Password] = "admin";
SessionFactory factory = SessionFactory.NewInstance();
ISession session = factory.GetRepositories(parameters)[0].CreateSession();

IFolder root = session.GetRootFolder();
Console.WriteLine(root.Name);

// Would have been normally better to use PortCMIS as it is supposed to be a more modern
// replacement of DotCMIS. But I could not pass the authentication exception, either in browser
// or in atom binding modes. Problem seems to be related to a new behaviour of Alfresco explained here:
// https://hub.alfresco.com/t5/alfresco-content-services-forum/alfresco-6-0-and-cmis-1-1-atompub-service-document/td-p/250359

// using PortCMIS;
// using PortCMIS.Client;
// using PortCMIS.Client.Impl;

// IDictionary<string, string> parameters = new Dictionary<string, string>() {
//     {SessionParameter.BindingType, BindingType.AtomPub},
//     {SessionParameter.AtomPubUrl, "http://edm:8080/alfresco/api/-default-/public/cmis/versions/1.1/atom"},
//     {SessionParameter.BrowserUrl, "http://edm:8080/alfresco/api/-default-/public/cmis/versions/1.1/browser/root"},
//     {SessionParameter.RepositoryId, "-default-"},
//     {SessionParameter.User, "admin"},
//     {SessionParameter.Password, "admin"}
// };

// ISessionFactory factory = SessionFactory.NewInstance();
// ISession session = factory.CreateSession(parameters);
