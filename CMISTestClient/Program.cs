﻿using DotCMIS;
using DotCMIS.Client;
using DotCMIS.Client.Impl;
using DotCMIS.Data.Impl;

Dictionary<string, string> parameters = new Dictionary<string, string>();
parameters[DotCMIS.SessionParameter.BindingType] = BindingType.AtomPub;
parameters[DotCMIS.SessionParameter.AtomPubUrl] = "http://127.0.0.1:8080/alfresco/api/-default-/public/cmis/versions/1.0/atom";
parameters[DotCMIS.SessionParameter.User] = "admin";
parameters[DotCMIS.SessionParameter.Password] = "admin";
SessionFactory factory = SessionFactory.NewInstance();
ISession session = factory.GetRepositories(parameters)[0].CreateSession();

IFolder root = session.GetRootFolder();
Console.WriteLine(root.Name);
