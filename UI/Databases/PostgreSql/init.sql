
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
    "activity_id" uuid,
    "start_time" timestamp NOT NULL,
    "end_time" timestamp NOT NULL,
    "input" jsonb,
    "result_status" workflow_step_result_status,
    "error_message" text,
    "result" jsonb,
    PRIMARY KEY ("id", "workflow_id", "namespace_id", "step_id"),
    FOREIGN KEY ("id", "workflow_id", "namespace_id") REFERENCES "archived_workflows" ("id", "workflow_id", "namespace_id") ON DELETE CASCADE,
    FOREIGN KEY ("activity_id", "namespace_id") REFERENCES "activities" ("id", "namespace_id") ON DELETE CASCADE
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

CREATE TABLE IF NOT EXISTS "activity_performance_raw" (
    "id" uuid NOT NULL,
    "window" timestamptz NOT NULL,
    "activity_id" uuid NOT NULL,
    "namespace_id" uuid NOT NULL,
    "started" bigint NOT NULL,
    "completed" bigint NOT NULL,
    "failed" bigint NOT NULL,
    "timed_out" bigint NOT NULL,
    "queue_latencies" jsonb NOT NULL,
    "durations" jsonb NOT NULL,
    PRIMARY KEY ("id"),
    FOREIGN KEY ("activity_id", "namespace_id") REFERENCES "activities" ("id", "namespace_id") ON DELETE CASCADE
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

CREATE TABLE IF NOT EXISTS "workflow_performance_raw" (
    "id" uuid NOT NULL,
    "window" timestamptz NOT NULL,
    "workflow_id" uuid NOT NULL,
    "namespace_id" uuid NOT NULL,
    "started" bigint NOT NULL,
    "completed" bigint NOT NULL,
    "failed" bigint NOT NULL,
    "purged" bigint NOT NULL,
    "queue_latencies" jsonb NOT NULL,
    PRIMARY KEY ("id"),
    FOREIGN KEY ("workflow_id", "namespace_id") REFERENCES "workflows" ("id", "namespace_id") ON DELETE CASCADE
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

CREATE OR REPLACE PROCEDURE "register_namespace" (IN p_name character varying)
LANGUAGE plpgsql 
AS $$
BEGIN
	IF NOT EXISTS (SELECT 1 FROM namespaces WHERE name = p_name ) THEN
		INSERT INTO namespaces (id, name, enabled) VALUES (gen_random_uuid(), p_name , TRUE);
	ELSE 
		UPDATE namespaces SET enabled = TRUE WHERE name = p_name;
	END IF;
END;
$$;

CREATE OR REPLACE PROCEDURE "unregister_namespace" (IN p_name character varying)
LANGUAGE plpgsql 
AS $$
BEGIN
	UPDATE namespaces SET enabled = FALSE WHERE name = p_name;
END;
$$;

CREATE OR REPLACE PROCEDURE "add_activity_performance_entry" (
    IN p_entry_window timestamptz, 
    IN p_namespace character varying ,
    IN p_activity character varying ,
    IN p_started bigint,
    IN p_completed bigint,
    IN p_failed bigint,
    IN p_timed_out bigint,
    IN p_queue_latencies jsonb,
    IN p_durations jsonb
)
LANGUAGE plpgsql 
AS $$
DECLARE
    v_namespace_id uuid;
    v_activity_id uuid;
BEGIN
	IF NOT EXISTS (SELECT 1 FROM namespaces WHERE name = p_namespace) THEN
        v_namespace_id := gen_random_uuid();
		INSERT INTO namespaces (id, name) VALUES (v_namespace_id, p_namespace);
    ELSE
        SELECT id INTO v_namespace_id FROM namespaces WHERE name = p_namespace;
	END IF;
    IF NOT EXISTS (SELECT 1 FROM activities WHERE name = p_activity AND namespace_id = v_namespace_id) THEN
        v_activity_id := gen_random_uuid();
        INSERT INTO activities (id, namespace_id, name) VALUES (v_activity_id, v_namespace_id, p_activity);
    ELSE
        SELECT id INTO v_activity_id FROM activities WHERE name = p_activity AND namespace_id = v_namespace_id;
    END IF;
    INSERT INTO activity_performance_raw (id, "window", namespace_id, activity_id, started, completed, failed, timed_out, queue_latencies, durations) 
    VALUES (gen_random_uuid(), p_entry_window, v_namespace_id, v_activity_id, p_started, p_completed, p_failed, p_timed_out, p_queue_latencies, p_durations);
END;
$$;

CREATE OR REPLACE PROCEDURE "add_workflow_performance_entry" (
    IN p_entry_window timestamptz,
    IN p_namespace character varying ,
    IN p_workflow character varying ,
    IN p_started bigint,
    IN p_completed bigint,
    IN p_failed bigint,
    IN p_purged bigint,
    IN p_queue_latencies jsonb
)
LANGUAGE plpgsql 
AS $$
DECLARE
    v_namespace_id uuid;
    v_workflow_id uuid;
BEGIN
	IF NOT EXISTS (SELECT 1 FROM namespaces WHERE name = p_namespace) THEN
        v_namespace_id := gen_random_uuid();
		INSERT INTO namespaces (id, name) VALUES (v_namespace_id, p_namespace);
    ELSE
        SELECT id INTO v_namespace_id FROM namespaces WHERE name = p_namespace;
	END IF;
    IF NOT EXISTS (SELECT 1 FROM workflows WHERE name = p_workflow AND namespace_id = v_namespace_id) THEN
        v_workflow_id := gen_random_uuid();
        INSERT INTO workflows (id, namespace_id, name) VALUES (v_workflow_id, v_namespace_id, p_workflow);
    ELSE
        SELECT id INTO v_workflow_id FROM workflows WHERE name = p_workflow AND namespace_id = v_namespace_id;
    END IF;
    INSERT INTO workflow_performance_raw (id, "window", namespace_id, workflow_id, started, completed, failed, purged, queue_latencies) 
    VALUES (gen_random_uuid(), p_entry_window, v_namespace_id, v_workflow_id, p_started, p_completed, p_failed, p_purged, p_queue_latencies);
END;
$$;

CREATE OR REPLACE PROCEDURE "create_archived_workflow" (
    IN p_id uuid ,
    IN p_namespace character varying ,
    IN p_workflow character varying ,
    IN p_scheduler_id uuid,
    IN p_started_at timestamp ,
    IN p_finished_at timestamp ,
    IN p_is_successful boolean ,
    IN p_error_message text,
    IN p_arguments jsonb
)
LANGUAGE plpgsql 
AS $$
DECLARE
    v_namespace_id uuid;
    v_workflow_id uuid;
BEGIN
	IF NOT EXISTS (SELECT 1 FROM namespaces WHERE name = p_namespace) THEN
        v_namespace_id := gen_random_uuid();
		INSERT INTO namespaces (id, name) VALUES (v_namespace_id, p_namespace);
    ELSE
        SELECT id INTO v_namespace_id FROM namespaces WHERE name = p_namespace;
	END IF;
    IF NOT EXISTS (SELECT 1 FROM workflows WHERE name = p_workflow AND namespace_id = v_namespace_id) THEN
        v_workflow_id := gen_random_uuid();
        INSERT INTO workflows (id, namespace_id, name) VALUES (v_workflow_id, v_namespace_id, p_workflow);
    ELSE
        SELECT id INTO v_workflow_id FROM workflows WHERE name = p_workflow AND namespace_id = v_namespace_id;
    END IF;
    IF NOT EXISTS (SELECT 1 FROM archived_workflows WHERE id = p_id AND workflow_id = v_workflow_id AND namespace_id = v_namespace_id) THEN
        INSERT INTO archived_workflows (id, workflow_id, namespace_id, scheduler_id, started_at, finished_at, is_successful, error_message, arguments) 
        VALUES (p_id, v_workflow_id, v_namespace_id, p_scheduler_id, p_started_at, p_finished_at, p_is_successful, p_error_message, p_arguments);
    END IF;
END;
$$;

CREATE OR REPLACE PROCEDURE "set_archived_workflow_options" (
    IN p_id uuid ,
    IN p_namespace character varying ,
    IN p_workflow character varying ,
    IN p_completion_action workflow_completion_actions ,
    IN p_purge_delay character varying(128),
    IN p_error_on_activity_timeout boolean ,
    IN p_error_on_activity_failure boolean 
)
LANGUAGE plpgsql 
AS $$
DECLARE
    v_namespace_id uuid;
    v_workflow_id uuid;
BEGIN
	IF NOT EXISTS (SELECT 1 FROM namespaces WHERE name = p_namespace) THEN
        v_namespace_id := gen_random_uuid();
		INSERT INTO namespaces (id, name) VALUES (v_namespace_id, p_namespace);
    ELSE
        SELECT id INTO v_namespace_id FROM namespaces WHERE name = p_namespace;
	END IF;
    IF NOT EXISTS (SELECT 1 FROM workflows WHERE name = p_workflow AND namespace_id = v_namespace_id) THEN
        v_workflow_id := gen_random_uuid();
        INSERT INTO workflows (id, namespace_id, name) VALUES (v_workflow_id, v_namespace_id, p_workflow);
    ELSE
        SELECT id INTO v_workflow_id FROM workflows WHERE name = p_workflow AND namespace_id = v_namespace_id;
    END IF;
    IF NOT EXISTS (SELECT 1 FROM archived_workflow_options WHERE id = p_id AND workflow_id = v_workflow_id AND namespace_id = v_namespace_id) THEN
        INSERT INTO archived_workflow_options (id, workflow_id, namespace_id, completion_action, purge_delay, error_on_activity_timeout, error_on_activity_failure) 
        VALUES (p_id, v_workflow_id, v_namespace_id, p_completion_action, p_purge_delay, p_error_on_activity_timeout, p_error_on_activity_failure);
    END IF;
END;
$$;

CREATE OR REPLACE PROCEDURE "add_archived_workflow_metadata_entry" (
    IN p_id uuid ,
    IN p_namespace character varying ,
    IN p_workflow character varying ,
    IN p_key_name character varying(512) ,
    IN p_value_index integer ,
    IN p_value text 
)
LANGUAGE plpgsql 
AS $$
DECLARE
    v_namespace_id uuid;
    v_workflow_id uuid;
BEGIN
	IF NOT EXISTS (SELECT 1 FROM namespaces WHERE name = p_namespace) THEN
        v_namespace_id := gen_random_uuid();
		INSERT INTO namespaces (id, name) VALUES (v_namespace_id, p_namespace);
    ELSE
        SELECT id INTO v_namespace_id FROM namespaces WHERE name = p_namespace;
	END IF;
    IF NOT EXISTS (SELECT 1 FROM workflows WHERE name = p_workflow AND namespace_id = v_namespace_id) THEN
        v_workflow_id := gen_random_uuid();
        INSERT INTO workflows (id, namespace_id, name) VALUES (v_workflow_id, v_namespace_id, p_workflow);
    ELSE
        SELECT id INTO v_workflow_id FROM workflows WHERE name = p_workflow AND namespace_id = v_namespace_id;
    END IF;
    IF NOT EXISTS (SELECT 1 FROM archived_workflow_metadata_entry WHERE id = id AND workflow_id = v_workflow_id AND namespace_id = v_namespace_id AND key_name = p_key_name AND value_index = p_value_index) THEN
        INSERT INTO archived_workflow_metadata_entry (id, workflow_id, namespace_id, key_name, value_index, value) 
        VALUES (p_id, v_workflow_id, v_namespace_id, p_key_name, p_value_index, p_value);
    END IF;
END;
$$;

CREATE OR REPLACE PROCEDURE "add_archived_workflow_step" (
    IN p_id uuid ,
    IN p_namespace character varying ,
    IN p_workflow character varying ,
    IN p_step_id bigint ,
    IN p_step_type workflow_step_types ,
    IN p_step_index bigint,
    IN p_step_name character varying(512),
    IN p_start_time timestamp ,
    IN p_end_time timestamp ,
    IN p_input jsonb,
    IN p_result_status workflow_step_result_status,
    IN p_error_message text,
    IN p_result jsonb
)
LANGUAGE plpgsql 
AS $$
DECLARE
    v_namespace_id uuid;
    v_workflow_id uuid;
    v_activity_id uuid;
BEGIN
	IF NOT EXISTS (SELECT 1 FROM namespaces WHERE name = p_namespace) THEN
        v_namespace_id := gen_random_uuid();
		INSERT INTO namespaces (id, name) VALUES (v_namespace_id, p_namespace);
    ELSE
        SELECT id INTO v_namespace_id FROM namespaces WHERE name = p_namespace;
	END IF;
    IF NOT EXISTS (SELECT 1 FROM workflows WHERE name = p_workflow AND namespace_id = v_namespace_id) THEN
        v_workflow_id := gen_random_uuid();
        INSERT INTO workflows (id, namespace_id, name) VALUES (v_workflow_id, v_namespace_id, p_workflow);
    ELSE
        SELECT id INTO v_workflow_id FROM workflows WHERE name = p_workflow AND namespace_id = v_namespace_id;
    END IF;
    IF step_name IS NOT NULL THEN
        IF NOT EXISTS (SELECT 1 FROM activities WHERE name = p_step_name AND namespace_id = v_namespace_id) THEN
            v_activity_id := gen_random_uuid();
            INSERT INTO activities (id, namespace_id, name) VALUES (v_activity_id, v_namespace_id, p_step_name);
        ELSE
            SELECT id INTO v_activity_id FROM activities WHERE name = p_step_name AND namespace_id = v_namespace_id;
        END IF;
    ELSE
        v_activity_id := NULL;
    END IF;
    IF NOT EXISTS (SELECT 1 FROM archived_workflow_metadata_entry WHERE id = p_id AND workflow_id = v_workflow_id AND namespace_id = v_namespace_id AND step_id = p_step_id) THEN
        INSERT INTO archived_workflow_steps (id, workflow_id, namespace_id, step_id, step_type, step_index, activity_id, start_time, end_time, input, result_status, error_message, result) 
        VALUES (p_id, v_workflow_id, v_namespace_id, p_step_id, p_step_type, p_step_index, v_activity_id, p_start_time, p_end_time, p_input, p_result_status, p_error_message, p_result);
    END IF;
END;
$$;

CREATE OR REPLACE PROCEDURE "add_archived_workflow_step_retry" (
    IN p_id uuid ,
    IN p_namespace character varying ,
    IN p_workflow character varying ,
    IN p_step_id bigint ,
    IN p_retry_index integer ,
    IN p_retry_type workflow_retry_types ,
    IN p_time_stamp timestamp 
)
LANGUAGE plpgsql 
AS $$
DECLARE
    v_namespace_id uuid;
    v_workflow_id uuid;
BEGIN
	IF NOT EXISTS (SELECT 1 FROM namespaces WHERE name = p_namespace) THEN
        v_namespace_id := gen_random_uuid();
		INSERT INTO namespaces (id, name) VALUES (v_namespace_id, p_namespace);
    ELSE
        SELECT id INTO v_namespace_id FROM namespaces WHERE name = p_namespace;
	END IF;
    IF NOT EXISTS (SELECT 1 FROM workflows WHERE name = p_workflow AND namespace_id = v_namespace_id) THEN
        v_workflow_id := gen_random_uuid();
        INSERT INTO workflows (id, namespace_id, name) VALUES (v_workflow_id, v_namespace_id, p_workflow);
    ELSE
        SELECT id INTO v_workflow_id FROM workflows WHERE name = p_workflow AND namespace_id = v_namespace_id;
    END IF;
    IF NOT EXISTS (SELECT 1 FROM archived_workflow_metadata_entry WHERE id = p_id AND workflow_id = v_workflow_id AND namespace_id = v_namespace_id AND step_id = p_step_id AND retry_index = p_retry_index) THEN
        INSERT INTO archived_workflow_step_retries (id, workflow_id, namespace_id, step_id, retry_index, retry_type, time_stamp) 
        VALUES (p_id, v_workflow_id, v_namespace_id, p_step_id, p_retry_index, p_retry_type, p_time_stamp);
    END IF;
END;
$$;