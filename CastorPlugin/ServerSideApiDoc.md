```markdown
# NFT Works Candidate REST API Documentation

## Overview
This document provides details about the NFT Works Candidate REST API, which allows clients to manage NFT works candidates. The API supports creating, retrieving, updating, and deleting candidates.

## Base URL
```
http://<your-server-url>/api/nft-works-candidates
```

## Endpoints

### 1. Create a New NFT Works Candidate
- **POST** `/`
- **Description**: Creates a new NFT works candidate.
- **Request Body**:
  ```json
  {
    "field1": "value1",
    "field2": "value2",
    ...
  }
  ```
  *(Replace `field1`, `field2`, etc. with actual fields from `CreateNftWorksCandidateDto`.)*

- **Response**:
  - **201 Created**
  ```json
  {
    "id": "unique-id",
    "uploadedOn": "2023-10-01T00:00:00.000Z",
    ...
  }
  ```

### 2. Retrieve All NFT Works Candidates
- **GET** `/`
- **Description**: Retrieves a list of all NFT works candidates.
- **Response**:
  - **200 OK**
  ```json
  [
    {
      "id": "unique-id-1",
      "uploadedOn": "2023-10-01T00:00:00.000Z",
      ...
    },
    {
      "id": "unique-id-2",
      "uploadedOn": "2023-10-02T00:00:00.000Z",
      ...
    }
  ]
  ```

### 3. Retrieve a Single NFT Works Candidate
- **GET** `/:id`
- **Description**: Retrieves a specific NFT works candidate by ID.
- **Parameters**:
  - `id` (string): The ID of the candidate.
- **Response**:
  - **200 OK**
  ```json
  {
    "id": "unique-id",
    "uploadedOn": "2023-10-01T00:00:00.000Z",
    ...
  }
  ```
  - **404 Not Found**: If the candidate does not exist.

### 4. Update an NFT Works Candidate
- **PUT** `/:id`
- **Description**: Updates an existing NFT works candidate.
- **Parameters**:
  - `id` (string): The ID of the candidate.
- **Request Body**:
  ```json
  {
    "field1": "new-value1",
    "field2": "new-value2",
    ...
  }
  ```
- **Response**:
  - **200 OK**
  ```json
  {
    "id": "unique-id",
    "uploadedOn": "2023-10-01T00:00:00.000Z",
    ...
  }
  ```
  - **404 Not Found**: If the candidate does not exist.

### 5. Delete an NFT Works Candidate
- **DELETE** `/:id`
- **Description**: Deletes a specific NFT works candidate by ID.
- **Parameters**:
  - `id` (string): The ID of the candidate.
- **Response**:
  - **204 No Content**: If the deletion is successful.
  - **404 Not Found**: If the candidate does not exist.

## Error Handling
The API returns standard HTTP status codes to indicate the success or failure of requests. Common error responses include:
- **400 Bad Request**: Invalid input data.
- **404 Not Found**: Resource not found.
- **500 Internal Server Error**: Unexpected server error.

## Authentication
*If applicable, describe the authentication method (e.g., API keys, OAuth).*

## Example Usage
### Create a Candidate
```bash
curl -X POST http://<your-server-url>/api/nft-works-candidates -H "Content-Type: application/json" -d '{"field1": "value1", "field2": "value2"}'
```

### Get All Candidates
```bash
curl -X GET http://<your-server-url>/api/nft-works-candidates
```

### Get a Candidate by ID
```bash
curl -X GET http://<your-server-url>/api/nft-works-candidates/<id>
```

### Update a Candidate
```bash
curl -X PUT http://<your-server-url>/api/nft-works-candidates/<id> -H "Content-Type: application/json" -d '{"field1": "new-value1"}'
```

### Delete a Candidate
```bash
curl -X DELETE http://<your-server-url>/api/nft-works-candidates/<id>
```

## Conclusion
This API provides a simple interface for managing NFT works candidates. Ensure to handle errors appropriately and validate inputs when making requests.
```