---

# **Real-Time HL7 Lab Result Processing on AWS (.NET 8, Lambda, S3, DynamoDB)**

A fully serverless, real-time HL7 ORU^R01 processing pipeline built using **AWS Lambda**, **Amazon S3**, **DynamoDB**, and **.NET 8**.
This project takes raw HL7 v2.x lab result messages, parses them using **nHapi**, and stores clean, structured lab results in DynamoDB — without managing any servers.

This repository contains **Part 1** of a multi-part series.

---

# 🚀 **Features**

### ✅ Upload HL7 files to S3

Triggers the entire pipeline automatically.

### ✅ Serverless HL7 Processing with .NET 8

Lambda reads and parses ORU^R01 HL7 messages using nHapi.

### ✅ Clean DynamoDB Schema

Stores one result per OBX segment using:

* `PatientId` (PK)
* `OrderId#TestCode#Timestamp` (SK)

### ✅ Idempotent Writes

Duplicate HL7 uploads do not create duplicate results.

### ✅ Fully Automated Deployment

Using AWS SAM (`template.yaml`).

---

# 🏗 **Architecture Overview**

```
[Upload HL7 to S3]
          ↓
[S3 Event → Lambda (.NET 8)]
          ↓
[Parse HL7 ORU^R01 via nHapi]
          ↓
[Store results in DynamoDB]
          ↓
[CloudWatch Logs for monitoring]
```

Key AWS Services:

* **S3** → HL7 ingestion
* **Lambda** → processing engine
* **DynamoDB** → structured storage
* **CloudWatch** → monitoring
* **SAM** → deployment

---

# 📁 **Project Structure**

```
LabResultProcessor/
 ├
 │    ├── LabResultProcessor.Core/         # HL7 parser + models (nHapi)
 │    └── LabResultProcessor.Function/     # Lambda function
 ├
 │    └── hl7FILES/                             # Sample ORU^R01 test files
 ├── template.yaml                         # AWS SAM deployment template
 └── README.md
```

---

# ⚙️ **Prerequisites**

You must have installed:

* **.NET 8 SDK**
* **AWS CLI** (`aws configure` must be completed)
* **AWS SAM CLI**
* An IAM user/role with rights to deploy:

  * Lambda
  * DynamoDB
  * S3
  * IAM roles

---

# 📦 **Setup & Deployment**

### 1️⃣ Clone the repository

```bash
git clone https://github.com/surajgadhvicloud/LabResultProcessor.git
cd lab-result-processor
```

### 2️⃣ Build using SAM

```bash
sam build
```

### 3️⃣ Deploy the stack

```bash
sam deploy --guided
```

During the first deploy, SAM will ask for:

* Stack name
* AWS region
* Permission to create IAM roles
* Whether to save deployment settings

After deployment, you will get:

* Lambda function URL
* S3 bucket name
* DynamoDB table name

---

# 🧪 **Testing the Pipeline**

### 1️⃣ Upload a sample HL7 file to S3

```bash
aws s3 cp samples/hl7/sample1.hl7 s3://<your-bucket-name>/
```

### 2️⃣ Check CloudWatch logs

You should see logs like:

```
Parsed 2 results from sample1.hl7
Saved result ORD123#HB#20250101100500 for patient 123456
```

### 3️⃣ Verify results in DynamoDB

Open the **LabResults** table and check the items.

---

# 🔍 **HL7 Messages Supported**

This project supports **HL7 v2.5.1 ORU^R01** messages containing:

* `PID` → Patient demographics
* `OBR` → Order details
* `OBX` → Individual test results


---

# 🧠 **How the HL7 Parsing Works**

HL7 parsing is done in:

```
src/LabResultProcessor.Core/Hl7LabResultParser.cs
```

The parser:

* Loads ORU^R01 messages via `PipeParser`
* Reads PID → patient details
* Reads OBR → order ID
* Reads OBX → test code, description, value, units, flags, timestamps
* Produces a list of `LabResult` objects

Each result is written to DynamoDB via the Lambda function.

---

# 📈 **DynamoDB Schema**

**Partition Key (PK):**

```
PatientId
```

**Sort Key (SK):**

```
OrderId#TestCode#Timestamp
```

This enables:

* Fetching all results for a patient
* NATURAL ordering by timestamp
* Idempotent writes on duplicate HL7 uploads

---

# 🛠 **AWS SAM Template**

The entire infrastructure is defined in:

```
template.yaml
```

It includes:

* Lambda Function
* S3 bucket (file upload trigger)
* DynamoDB table
* IAM permissions
* Event mapping

---

# 🔮 **What’s Coming in Part 2**

In the next part of the series, we will add a user-facing experience:

### **Part 2 Features**

* Secure **login using AWS Cognito**
* Web UI to **upload HL7 files**
* API Gateway to validate and forward uploads to S3
* Cleaner user workflow from upload → processing

### **Part 3 Preview**

* DynamoDB → SQL ETL pipeline
* Normalized SQL database schema
* UI to view lab results as human-friendly reports

---

# 🤝 **Contributing**

Feel free to:

* Open issues
* Submit PRs
* Suggest improvements
* Add support for more HL7 message types

---
