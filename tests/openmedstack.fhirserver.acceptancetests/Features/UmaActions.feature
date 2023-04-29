Feature: UMA resource interactions
User interactions with UMA resource sets

    Background: Normal running server
        Given a running server setup
        And a token provider
        And a UMA FHIR client

    Scenario: Read FHIR resource with UMA permission
        Given FHIR resource registered as a UMA resource set
        And a valid UMA token
        When the resource is requested without an id token
        Then the resource is returned

    Scenario: Read FHIR resource with no UMA permission
        Given FHIR resource registered as a UMA resource set
        And an invalid UMA token
        When the resource is requested without an id token
        Then an UMA error is returned

    Scenario: Read FHIR resource with no UMA permission but with id token
        Given FHIR resource registered as a UMA resource set
        And an invalid UMA token
        When the resource is requested with an id token
        Then an UMA ticket is returned
