# Enterprise Architecture with .NET

This repository contains all the files associated with the book `Enterprise Architecture with .NET`, published by Packt. This book aims at providing a method to create industrial-grade Information Systems that will stay easy to evolve in time, owing to a business/IT alignment method and the use of modularization of services and standardization.

## DemoEditor

The most important folder is the one with the sample application the book is written around. In order to help the readers follow the example, the application is presented in several successive versions, each being based on the previous one and adding some more complexity. Github branches are used for readers to be able to quickly shift from one version to another. The `README.md` file inside the [DemoEditor](./DemoEditor/) folder adjusts to each version with the prerequisites and install operations to be realized.

**DemoEditor** is the arbitrary name chosen for a company that would create a modern Information System based on its legacy applications and design it following the method presented in the book, with a strict separation of responsibility and being largely based on existing norms and standards.

## OpenAPI

Since an important part of standardization and business-alignment is implemented by contract-first, OpenAPI-compatible, APIs, the YAML definitions of such contracts are of particular importance and provided in a separate folder from the implementation, as they have their own lifecycle and versioning.

## OPA-ABAC-Sample

The book explains how to separate Business Rules Management from the rest of the implementations of the APIs, and the files in this folder show how to realize such a separation using Open Policy Agent (https://www.openpolicyagent.org/).

## OPA-SpecFlowTests

Automated tests using a Behaviour-Driven Design approach complements the Open Policy Agent implementation, and are provided in a dedicated folder, as a SpecFlow-based project (https://specflow.org/ for more details).

## BPMN

This folder contains the exports of a few business processes used as examples. The `.bpmn` files can be opened with any BPMN 2.0 compatible editor, for example https://demo.bpmn.io/.

## CMISTestClient

A small .NET client that shows how to use CMIS standard to access an Electronic Document Management system.