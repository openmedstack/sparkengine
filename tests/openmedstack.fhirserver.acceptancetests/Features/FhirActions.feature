Feature: FHIR API Conformance and Security Testing
Tests that the FHIR API actions are working as expected

    Background: Setup
        Given a running server setup
        And a token provider
        And a FHIR client
        And I have a valid patient resource with the following parameters
          | id  | first_name | last_name | gender |
          | 123 | John       | Smith     | male   |

    Scenario: Retrieve a patient resource by ID
        When I send a GET request to /Patient/123
        Then the response should have a status code of 200
        And the response body should contain a patient resource with id 123

    Scenario: Search for patients by name
        When I send a GET request to /Patient with the following parameters
          | first_name | last_name | gender |
          | John       | Smith     | male   |
        Then the response should have a status code of 200
        And the response body should contain a bundle of patient resources that match the criteria

    Scenario: Invalid Access Token
        When a GET request is made to the FHIR API with an invalid token
        Then the response should have a status code of 401
        And the response should contain an error message indicating authentication failure

    Scenario: Invalid Resource ID
        When a GET request is made to the FHIR API with the invalid resource ID
        Then the response should have a status code of 404
        And the response should contain an error message indicating resource not found

    Scenario: Invalid Search Criteria
        When a GET request is made to the FHIR API with an invalid parameter and value
        Then the response should have a status code of 400
        And the response should contain an error message indicating invalid search criteria

    Scenario: Missing Required Parameters
        When a GET request is made to the FHIR API with incomplete request parameters
        Then the response should contain an error message indicating missing or invalid request parameters
