# WebSocket Usage Documentation
> [!NOTE]
> The server will discard any data you send to the WebSocket, as it is only used to provide clients with live feedback on certain tasks that are ongoing on the server.

## Endpoint
The WebSocket endpoint is available under `/ws` of the root of your website.

## Response Format
The WebSocket will always send requests formatted in ``JSON`` using the following format:
```JSON
{
  "key": "", // The key tell the client how to handle the "data" field.
  "data": {} // This field data type change according to the key value.
}
```

## Existing Response Keys
This is a list of the existing response keys along with the data types that can be returned by the server.

### NewJobWasQueued
`NewJobWasQueued` is used when a job get added to the queued list of the JobService responsible for executing jobs.
The ``data`` field is an object type containing a `JobModel`, schema available on Swagger docs.

### JobReportedStatusUpdate
`JobReportedStatusUpdate` is used when a job status get updated by the server.
The ``data`` field is an object type in this format:
```JSON
{
  "id": "", // This is the job ID that got a status update.
  "progress": 0, // Job progress on a 0-100 scale.
  "progressTaskName": "", // Name or Description of the task currently being done during the job execution.
  "status": "", // Status of the job, all status available on Swagger schema.
}
```

### JobDestroyed
`JobDestroyed` is used when a job is disposed from the server memory.
The ``data`` field is an string type containing the disposed Job ID.
