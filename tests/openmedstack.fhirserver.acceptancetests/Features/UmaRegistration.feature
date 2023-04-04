Feature: Registration of created FHIR resources as UMA resources

Background: Normal running server
	Given a running server setup
	And a FHIR client

Scenario: Register new FHIR resource as UMA resource
	Given a FHIR resource
	When the resource is created
	Then the resource is registered as a UMA resource
