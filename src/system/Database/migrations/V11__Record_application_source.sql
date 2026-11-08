ALTER TABLE applications ADD COLUMN source text NOT NULL DEFAULT 'Client website';
ALTER TABLE applications ALTER COLUMN source DROP DEFAULT;
