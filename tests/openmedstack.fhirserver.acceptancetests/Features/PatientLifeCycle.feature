Feature: PatientLifeCycle

Resource creation, update and deletion lifecycle

    Background:
        Given a running server setup
        And a token provider
        And a FHIR client

    Scenario: Patient Lifecycle
        When creating a patient resource
        Then patient has id
        And patient can be updated
        And can be found when searched
