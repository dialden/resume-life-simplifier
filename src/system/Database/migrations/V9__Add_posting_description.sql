ALTER TABLE postings ADD COLUMN description text NOT NULL DEFAULT '';
ALTER TABLE postings ALTER COLUMN description DROP DEFAULT;
