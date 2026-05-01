# Smart Document Processing System  

A web application developed in ASP.NET Core 10 MVC, designed to automate the processing of incoming documents (invoices, purchase orders, and text reports). 
The system enables rapid uploading, data extraction, and document validation to minimize the need for manual data entry.

## Key Features

## Automated Document Processing
The application supports three primary file formats:
* PDF: Advanced text extraction using `iText7`. The system automatically recognizes the supplier, document number, dates, and amounts.
* CSV: Fast import of structured data with automatic item mapping and total value calculations.
* TXT: Processing of simple text formats using optimized Regular Expressions (Regex).

## Validation
After processing, every document undergoes additional quality control:
* Mathematical Check: The system verifies if the sum of all line items matches the total amount (Subtotal + Tax = Total).
* Duplicate Detection: Entry of documents with duplicate numbers is disabled to prevent accounting errors.
* Integrity Check: Automatic identification of missing fields (e.g., unknown supplier or missing due date).

## Document Management
* Status System: Documents are classified (e.g., Uploaded, Needs Review, Validated, Rejected) based on validation results.
* Upload History: Each document is securely stored in a local `Uploads` folder, while metadata is simultaneously saved in the database.

## Technology Stack
* Backend: .NET 10 / ASP.NET Core MVC
* Database: Entity Framework Core (SQL Server)
* Text Extraction: iText7 (for PDF format)
* CSV Parsing: CsvHelper
* Performance: Optimized `static readonly` Regex expressions with compilation for faster text processing.

## Installation and Setup
1. Clone the repository:
    git clone https://github.com/your-username/SDPWebApp.git

2. Database Configuration:
    Check `appsettings.json` and configure your Connection String, then run the migrations:
    dotnet ef database update

3.  Run the application:
    dotnet run

## Implemented Optimizations ( Refactoring )
The following advanced C# practices were implemented during development:
* Pattern Matching: Utilizing the modern `is` operator for safe type conversion.
* Collection Expressions: Using `[]` for list initialization to improve code readability.
* Static Methods: Key processing methods are marked as `static` for better performance and memory efficiency.
* Regex Optimization: Instead of instantiating new objects within loops, static expression compilation was used.

## Author
* Edis Vrtagić
* This project was developed as a technical test assignment and, as such, reserves the full right to be published for public purposes.