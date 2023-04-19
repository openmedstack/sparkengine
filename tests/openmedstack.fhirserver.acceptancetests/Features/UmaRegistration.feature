Feature: Registration of created FHIR resources as UMA resources

Background: Normal running server
	Given a running server setup
	And a FHIR client

Scenario: Register new FHIR resource as UMA resource
	Given a FHIR resource
	When the resource is created
	And the user registers it as a resource set
	Then the resource is registered as a UMA resource
