---
title: "9: Autobarn Exercises"
layout: module
nav_order: 9
summary: >
    Extra exercises and services you can add to the Autobarn application to get further experience of working with distributed applications
---



### Exercise: Autobarn.MailSender

Create an additional service as part of the Autobarn stack, that will send an email every time we get the price for a new vehicle.

The service should:

* Subscribe to `NewVehiclePriceMessage` messages
* Use the `Microsoft.Extensions.Hosting` package and run as a hosted service

Use an online service such as [Mailtrap](https://mailtrap.io/) to get a test SMTP connection you can use to send (and view) email messages.

### Exercise: Vehicle Condition

We’ve been asked to include the condition of the vehicle in the Autobarn listing process.

Vehicles can be:

* New
* Used (Good)
* Used (Fair)
* Used (Poor)
* Non-runner
* Write-off

The vehicle condition needs to be reflected at every stage of the process:

* The HTTP API exposed on the Autobarn website should require the vehicle condition to be specified when listing a new vehicle for sale
  * Question: how should the client specify the condition?
  * Question: does the API need to expose a list of vehicle conditions somewhere?
* The `NewVehicleMessage` publishers and subscribers will need to capture and preserve this new field when publishing messages about new vehicles
* The `PricingServer` should take the vehicle condition into account when calculating prices
  * Exactly how we do this is up to you :)
* The website notifications should include the condition of the vehicle in the pop-up that’s displayed to website visitors

#### Exercise: Vehicle Status

There’s an online service that will let us check the reported status of a vehicle:

`https://ursatile-vehicle-info-checker.azurewebsites.net/api/CheckVehicleStatus?registration={reg}`

This service will return a plain text string response:

* `OK` - The vehicle is OK
* `STOLEN` - the vehicle has been reported stolen
* `WRITTEN_OFF` - the vehicle has been registered as written off
* `INVALID` - you have not specified a valid vehicle registration

Add a new microservice called `Autobarn.StatusChecker` to the Autobarn application which will query a vehicle status BEFORE calculating the vehicle price.

* If the vehicle is reported stolen, the status checker service should use the HTTP API and send a `DELETE` request to remove the vehicle from the database
* If the vehicle is written off, ensure the status in the NewVehicleMessage reflects this.
* Your service should publish a `VehicleStatusValidated` message
* You’ll need to update the `Autobarn.PricingClient` to subscribe to `VehicleStatusValidated`, so that we only calculate a price AFTER we’ve confirmed the vehicle status.





 

 
