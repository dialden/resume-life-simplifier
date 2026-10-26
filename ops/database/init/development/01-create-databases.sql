\set ON_ERROR_STOP on

\getenv dev_database RLS_DEV_DATABASE
\getenv test_database RLS_TEST_DATABASE

CREATE DATABASE :"dev_database";
CREATE DATABASE :"test_database";
