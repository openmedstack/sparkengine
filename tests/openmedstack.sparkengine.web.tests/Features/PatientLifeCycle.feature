Feature: PatientLifeCycle

Patient creation, update and deletion lifecycle

@tag1
Scenario: Patient Lifecycle
	Given a running server
	And a FHIR client
	When creating a patient resource
	Then patient has id
	And patient can be updated
	And can be found when searched
