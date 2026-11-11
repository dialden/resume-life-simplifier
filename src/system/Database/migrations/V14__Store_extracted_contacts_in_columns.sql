ALTER TABLE applications
    ADD COLUMN extracted_name text NULL,
    ADD COLUMN extracted_email text NULL,
    ADD COLUMN extracted_phone_number text NULL,
    ADD COLUMN extracted_postal_address text NULL,
    ADD COLUMN extracted_linkedin_url text NULL;

UPDATE applications
SET extracted_name = extracted_contacts ->> 'name',
    extracted_email = extracted_contacts ->> 'email',
    extracted_phone_number = extracted_contacts ->> 'phoneNumber',
    extracted_postal_address = extracted_contacts ->> 'postalAddress',
    extracted_linkedin_url = extracted_contacts ->> 'linkedInUrl'
WHERE extracted_contacts IS NOT NULL;

ALTER TABLE applications
    DROP CONSTRAINT applications_extraction_result_check,
    DROP COLUMN extracted_contacts,
    ADD CONSTRAINT applications_extraction_result_check
        CHECK (
            extraction_status = 'Completed'
            OR (extracted_name IS NULL
                AND extracted_email IS NULL
                AND extracted_phone_number IS NULL
                AND extracted_postal_address IS NULL
                AND extracted_linkedin_url IS NULL)
        );
