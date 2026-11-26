UPDATE application_education
SET qualification = CASE
    WHEN qualification IS NULL THEN field_of_study
    WHEN field_of_study IS NULL THEN qualification
    ELSE qualification || E'\n' || field_of_study
END;

ALTER TABLE application_education DROP COLUMN field_of_study;
