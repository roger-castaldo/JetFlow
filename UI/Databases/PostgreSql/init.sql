
DO $$ 
BEGIN
    CREATE TYPE "performance_aggregation_types" AS ENUM ('1Minute', '5Minute', '1Hour', '1Day');
EXCEPTION
    WHEN duplicate_object THEN NULL;
END $$;

DO $$ 
BEGIN
    CREATE TYPE "workflow_completion_actions" AS ENUM ('Archive', 'Purge');
EXCEPTION
    WHEN duplicate_object THEN NULL;
END $$;

DO $$ 
BEGIN
    CREATE TYPE "workflow_retry_types" AS ENUM ('Timeout', 'Error');
EXCEPTION
    WHEN duplicate_object THEN NULL;
END $$;

DO $$ 
BEGIN
    CREATE TYPE "workflow_step_result_status" AS ENUM ('Success', 'Failure', 'Timeout');
EXCEPTION
    WHEN duplicate_object THEN NULL;
END $$;


DO $$ 
BEGIN
    CREATE TYPE "workflow_step_types" AS ENUM ('Action', 'Delay', 'Suspended');
EXCEPTION
    WHEN duplicate_object THEN NULL;
END $$;

CREATE TABLE IF NOT EXISTS "namespaces" (
    "id" uuid NOT NULL,
    "name" character varying(512) UNIQUE,
    "enabled" boolean DEFAULT true NOT NULL,
    PRIMARY KEY ("id")
);

CREATE TABLE IF NOT EXISTS "activities" (
    "id" uuid NOT NULL,
    "namespace_id" uuid NOT NULL,
    "name" character varying(512) NOT NULL,
    PRIMARY KEY ("id", "namespace_id"),
    FOREIGN KEY ("namespace_id") REFERENCES "namespaces" ("id") ON DELETE CASCADE
);

CREATE TABLE IF NOT EXISTS "workflows" (
    "id" uuid NOT NULL,
    "namespace_id" uuid NOT NULL,
    "name" character varying(512) NOT NULL,
    PRIMARY KEY ("id", "namespace_id"),
    FOREIGN KEY ("namespace_id") REFERENCES "namespaces" ("id") ON DELETE CASCADE
);

CREATE TABLE IF NOT EXISTS "archived_workflows" (
    "id" uuid NOT NULL,
    "workflow_id" uuid,
    "namespace_id" uuid NOT NULL,
    "scheduler_id" uuid,
    "started_at" timestamp NOT NULL,
    "finished_at" timestamp NOT NULL,
    "is_successful" boolean NOT NULL,
    "error_message" text,
    "arguments" jsonb,
    PRIMARY KEY ("id", "workflow_id", "namespace_id"),
    FOREIGN KEY ("workflow_id", "namespace_id") REFERENCES "workflows" ("id", "namespace_id") ON DELETE CASCADE
);

CREATE TABLE IF NOT EXISTS "archived_workflow_metadata_entry" (
    "id" uuid NOT NULL,
    "workflow_id" uuid NOT NULL,
    "namespace_id" uuid NOT NULL,
    "key_name" character varying(512) NOT NULL,
    "value_index" integer NOT NULL,
    "value" text NOT NULL,
    PRIMARY KEY ("id", "workflow_id", "namespace_id", "key_name", "value_index"),
    FOREIGN KEY ("id", "workflow_id", "namespace_id") REFERENCES "archived_workflows" ("id", "workflow_id", "namespace_id") ON DELETE CASCADE
);

CREATE TABLE IF NOT EXISTS "archived_workflow_options" (
    "id" uuid NOT NULL,
    "workflow_id" uuid NOT NULL,
    "namespace_id" uuid NOT NULL,
    "completion_action" workflow_completion_actions NOT NULL,
    "purge_delay" character varying(128),
    "error_on_activity_timeout" boolean DEFAULT false NOT NULL,
    "error_on_activity_failure" boolean DEFAULT false NOT NULL,
    PRIMARY KEY ("id", "workflow_id", "namespace_id"),
    FOREIGN KEY ("id", "workflow_id", "namespace_id") REFERENCES "archived_workflows" ("id", "workflow_id", "namespace_id") ON DELETE CASCADE
);

CREATE TABLE IF NOT EXISTS "archived_workflow_steps" (
    "id" uuid NOT NULL,
    "workflow_id" uuid NOT NULL,
    "namespace_id" uuid NOT NULL,
    "step_id" bigint NOT NULL,
    "step_type" workflow_step_types NOT NULL,
    "step_index" bigint,
    "step_name" character varying(512),
    "start_time" timestamp NOT NULL,
    "end_time" timestamp NOT NULL,
    "input" jsonb,
    "result_status" workflow_step_result_status,
    "error_message" text,
    "result" jsonb,
    PRIMARY KEY ("id", "workflow_id", "namespace_id", "step_id"),
    FOREIGN KEY ("id", "workflow_id", "namespace_id") REFERENCES "archived_workflows" ("id", "workflow_id", "namespace_id") ON DELETE CASCADE
);

CREATE TABLE IF NOT EXISTS "archived_workflow_step_retries" (
    "id" uuid NOT NULL,
    "workflow_id" uuid NOT NULL,
    "namespace_id" uuid NOT NULL,
    "step_id" bigint NOT NULL,
    "retry_index" integer NOT NULL,
    "retry_type" workflow_retry_types NOT NULL,
    "time_stamp" timestamp NOT NULL,
    PRIMARY KEY ("id", "workflow_id", "namespace_id", "step_id", "retry_index"),
    FOREIGN KEY ("id", "workflow_id", "namespace_id", "step_id") REFERENCES "archived_workflow_steps" ("id", "workflow_id", "namespace_id", "step_id") ON DELETE CASCADE
);


CREATE TABLE IF NOT EXISTS "activity_performance" (
    "bucket_start" timestamptz NOT NULL,
    "activity_id" uuid NOT NULL,
    "namespace_id" uuid NOT NULL,
    "aggregation_type" performance_aggregation_types NOT NULL,
    "started" bigint NOT NULL,
    "completed" bigint NOT NULL,
    "failed" bigint NOT NULL,
    "timed_out" bigint NOT NULL,
    "queue_latency_digest" bytea NOT NULL,
    "duration_digest" bytea NOT NULL,
    PRIMARY KEY ("bucket_start", "activity_id", "namespace_id", "aggregation_type"),
    FOREIGN KEY ("activity_id", "namespace_id") REFERENCES "activities" ("id", "namespace_id") ON DELETE CASCADE
);

CREATE TABLE IF NOT EXISTS "workflow_performance" (
    "bucket_start" timestamptz NOT NULL,
    "workflow_id" uuid NOT NULL,
    "namespace_id" uuid NOT NULL,
    "aggregation_type" performance_aggregation_types NOT NULL,
    "started" bigint NOT NULL,
    "completed" bigint NOT NULL,
    "failed" bigint NOT NULL,
    "purged" bigint NOT NULL,
    "queue_latency_digest" bytea NOT NULL,
    PRIMARY KEY ("bucket_start", "workflow_id", "namespace_id", "aggregation_type"),
    FOREIGN KEY ("workflow_id", "namespace_id") REFERENCES "workflows" ("id", "namespace_id") ON DELETE CASCADE
);

CREATE OR REPLACE PROCEDURE "cleanup_performance_data" () 
LANGUAGE plpgsql 
AS $$
BEGIN
	DELETE FROM workflow_performance WHERE 
		( aggregation_type = '1Minute' AND bucket_start < NOW() - INTERVAL '3 days' )
		OR ( aggregation_type = '5Minute' AND bucket_start < NOW() - INTERVAL '15 days' )
		OR ( aggregation_type = '1Hour' AND bucket_start < NOW() - INTERVAL '30 days' )
		OR ( aggregation_type = '1Day' AND bucket_start < NOW() - INTERVAL '365 days' );
	DELETE FROM activity_performance WHERE 
		( aggregation_type = '1Minute' AND bucket_start < NOW() - INTERVAL '3 days' )
		OR ( aggregation_type = '5Minute' AND bucket_start < NOW() - INTERVAL '15 days' )
		OR ( aggregation_type = '1Hour' AND bucket_start < NOW() - INTERVAL '30 days' )
		OR ( aggregation_type = '1Day' AND bucket_start < NOW() - INTERVAL '365 days' );
END;
$$;

CREATE OR REPLACE PROCEDURE "register_namespace" (IN "name" character varying)
LANGUAGE plpgsql 
AS $$
BEGIN
	IF NOT EXISTS (SELECT 1 FROM namespaces WHERE name = name) THEN
		INSERT INTO namespaces (id, name) VALUES (gen_random_uuid(), name);
	ELSE 
		UPDATE namespaces SET enabled = TRUE WHERE name = name;
	END IF;
END;
$$;

CREATE OR REPLACE PROCEDURE "unregister_namespace" (IN "name" character varying)
LANGUAGE plpgsql 
AS $$
BEGIN
	UPDATE namespaces SET enabled = FALSE WHERE name = name;
END;
$$;

CREATE OR REPLACE PROCEDURE "create_archived_workflow" (
    IN "id" uuid ,
    IN "namespace" character varying ,
    IN "workflow" character varying ,
    IN "scheduler_id" uuid,
    IN "started_at" timestamp ,
    IN "finished_at" timestamp ,
    IN "is_successful" boolean ,
    IN "error_message" text,
    IN "arguments" jsonb
)
LANGUAGE plpgsql 
AS $$
DECLARE
    namespace_id uuid;
    workflow_id uuid;
BEGIN
	IF NOT EXISTS (SELECT 1 FROM namespaces WHERE name = name) THEN
        namespace_id := gen_random_uuid();
		INSERT INTO namespaces (id, name) VALUES (namespace_id, name);
    ELSE
        SELECT id INTO namespace_id FROM namespaces WHERE name = name;
	END IF;
    IF NOT EXISTS (SELECT 1 FROM workflows WHERE name = workflow AND namespace_id = namespace_id) THEN
        workflow_id := gen_random_uuid();
        INSERT INTO workflows (id, namespace_id, name) VALUES (workflow_id, namespace_id, workflow);
    ELSE
        SELECT id INTO workflow_id FROM workflows WHERE name = workflow AND namespace_id = namespace_id;
    END IF;
    IF NOT EXISTS (SELECT 1 FROM archived_workflows WHERE id = id AND workflow_id = workflow_id AND namespace_id = namespace_id) THEN
        INSERT INTO archived_workflows (id, workflow_id, namespace_id, scheduler_id, started_at, finished_at, is_successful, error_message, arguments) 
        VALUES (id, workflow_id, namespace_id, scheduler_id, started_at, finished_at, is_successful, error_message, arguments);
    END IF;
END;
$$;

CREATE OR REPLACE PROCEDURE "set_archived_workflow_options" (
    IN "id" uuid ,
    IN "namespace" character varying ,
    IN "workflow" character varying ,
    IN "completion_action" workflow_completion_actions ,
    IN "purge_delay" character varying(128),
    IN "error_on_activity_timeout" boolean ,
    IN "error_on_activity_failure" boolean 
)
LANGUAGE plpgsql 
AS $$
DECLARE
    namespace_id uuid;
    workflow_id uuid;
BEGIN
	IF NOT EXISTS (SELECT 1 FROM namespaces WHERE name = name) THEN
        namespace_id := gen_random_uuid();
		INSERT INTO namespaces (id, name) VALUES (namespace_id, name);
    ELSE
        SELECT id INTO namespace_id FROM namespaces WHERE name = name;
	END IF;
    IF NOT EXISTS (SELECT 1 FROM workflows WHERE name = workflow AND namespace_id = namespace_id) THEN
        workflow_id := gen_random_uuid();
        INSERT INTO workflows (id, namespace_id, name) VALUES (workflow_id, namespace_id, workflow);
    ELSE
        SELECT id INTO workflow_id FROM workflows WHERE name = workflow AND namespace_id = namespace_id;
    END IF;
    IF NOT EXISTS (SELECT 1 FROM archived_workflow_options WHERE id = id AND workflow_id = workflow_id AND namespace_id = namespace_id) THEN
        INSERT INTO archived_workflow_options (id, workflow_id, namespace_id, completion_action, purge_delay, error_on_activity_timeout, error_on_activity_failure) 
        VALUES (id, workflow_id, namespace_id, completion_action, purge_delay, error_on_activity_timeout, error_on_activity_failure);
    END IF;
END;
$$;

CREATE OR REPLACE PROCEDURE "add_archived_workflow_metadata_entry" (
    IN "id" uuid ,
    IN "namespace" character varying ,
    IN "workflow" character varying ,
    IN "key_name" character varying(512) ,
    IN "value_index" integer ,
    IN "value" text 
)
LANGUAGE plpgsql 
AS $$
DECLARE
    namespace_id uuid;
    workflow_id uuid;
BEGIN
	IF NOT EXISTS (SELECT 1 FROM namespaces WHERE name = name) THEN
        namespace_id := gen_random_uuid();
		INSERT INTO namespaces (id, name) VALUES (namespace_id, name);
    ELSE
        SELECT id INTO namespace_id FROM namespaces WHERE name = name;
	END IF;
    IF NOT EXISTS (SELECT 1 FROM workflows WHERE name = workflow AND namespace_id = namespace_id) THEN
        workflow_id := gen_random_uuid();
        INSERT INTO workflows (id, namespace_id, name) VALUES (workflow_id, namespace_id, workflow);
    ELSE
        SELECT id INTO workflow_id FROM workflows WHERE name = workflow AND namespace_id = namespace_id;
    END IF;
    IF NOT EXISTS (SELECT 1 FROM archived_workflow_metadata_entry WHERE id = id AND workflow_id = workflow_id AND namespace_id = namespace_id AND key_name = key_name AND value_index = value_index) THEN
        INSERT INTO archived_workflow_metadata_entry (id, workflow_id, namespace_id, key_name, value_index, value) 
        VALUES (id, workflow_id, namespace_id, key_name, value_index, value);
    END IF;
END;
$$;

CREATE OR REPLACE PROCEDURE "add_archived_workflow_step" (
    IN "id" uuid ,
    IN "namespace" character varying ,
    IN "workflow" character varying ,
    IN "step_id" bigint ,
    IN "step_type" workflow_step_types ,
    IN "step_index" bigint,
    IN "step_name" character varying(512),
    IN "start_time" timestamp ,
    IN "end_time" timestamp ,
    IN "input" jsonb,
    IN "result_status" workflow_step_result_status,
    IN "error_message" text,
    IN "result" jsonb
)
LANGUAGE plpgsql 
AS $$
DECLARE
    namespace_id uuid;
    workflow_id uuid;
BEGIN
	IF NOT EXISTS (SELECT 1 FROM namespaces WHERE name = name) THEN
        namespace_id := gen_random_uuid();
		INSERT INTO namespaces (id, name) VALUES (namespace_id, name);
    ELSE
        SELECT id INTO namespace_id FROM namespaces WHERE name = name;
	END IF;
    IF NOT EXISTS (SELECT 1 FROM workflows WHERE name = workflow AND namespace_id = namespace_id) THEN
        workflow_id := gen_random_uuid();
        INSERT INTO workflows (id, namespace_id, name) VALUES (workflow_id, namespace_id, workflow);
    ELSE
        SELECT id INTO workflow_id FROM workflows WHERE name = workflow AND namespace_id = namespace_id;
    END IF;
    IF NOT EXISTS (SELECT 1 FROM archived_workflow_metadata_entry WHERE id = id AND workflow_id = workflow_id AND namespace_id = namespace_id AND step_id = step_id) THEN
        INSERT INTO archived_workflow_steps (id, workflow_id, namespace_id, step_id, step_type, step_index, step_name, start_time, end_time, input, result_status, error_message, result) 
        VALUES (id, workflow_id, namespace_id, step_id, step_type, step_index, step_name, start_time, end_time, input, result_status, error_message, result);
    END IF;
END;
$$;

CREATE OR REPLACE PROCEDURE "add_archived_workflow_step_retry" (
    IN "id" uuid ,
    IN "namespace" character varying ,
    IN "workflow" character varying ,
    IN "step_id" bigint ,
    IN "retry_index" integer ,
    IN "retry_type" workflow_retry_types ,
    IN "time_stamp" timestamp 
)
LANGUAGE plpgsql 
AS $$
DECLARE
    namespace_id uuid;
    workflow_id uuid;
BEGIN
	IF NOT EXISTS (SELECT 1 FROM namespaces WHERE name = name) THEN
        namespace_id := gen_random_uuid();
		INSERT INTO namespaces (id, name) VALUES (namespace_id, name);
    ELSE
        SELECT id INTO namespace_id FROM namespaces WHERE name = name;
	END IF;
    IF NOT EXISTS (SELECT 1 FROM workflows WHERE name = workflow AND namespace_id = namespace_id) THEN
        workflow_id := gen_random_uuid();
        INSERT INTO workflows (id, namespace_id, name) VALUES (workflow_id, namespace_id, workflow);
    ELSE
        SELECT id INTO workflow_id FROM workflows WHERE name = workflow AND namespace_id = namespace_id;
    END IF;
    IF NOT EXISTS (SELECT 1 FROM archived_workflow_metadata_entry WHERE id = id AND workflow_id = workflow_id AND namespace_id = namespace_id AND step_id = step_id AND retry_index = retry_index) THEN
        INSERT INTO archived_workflow_step_retries (id, workflow_id, namespace_id, step_id, retry_index, retry_type, time_stamp) 
        VALUES (id, workflow_id, namespace_id, step_id, retry_index, retry_type, time_stamp);
    END IF;
END;
$$;