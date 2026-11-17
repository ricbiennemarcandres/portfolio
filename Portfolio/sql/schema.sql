-- =====================================================================
--  PORTFOLIO  ·  schema.sql
--  Database Management and Web Systems · Final Laboratory Exam
--
--  Run this in Supabase: Dashboard > SQL Editor > New query > Run.
--  It (re)creates the schema "portfolio" with three tables:
--
--      course  1 ──< project  1 ──< project_image
--
--  WARNING: the DROP statements below delete the tables and their data.
--  Run this file again only when you want to start over.
-- =====================================================================

create schema if not exists portfolio;

drop table if exists portfolio.project_image;
drop table if exists portfolio.project;
drop table if exists portfolio.course;


-- ---------------------------------------------------------------------
--  COURSE — a course I have taken.
--  code is the natural, unique identifier printed on the curriculum.
-- ---------------------------------------------------------------------
create table portfolio.course (
    course_id    integer      generated always as identity,
    code         varchar(16)  constraint nn_course_code        not null,
    title        varchar(120) constraint nn_course_title       not null,
    description  varchar(1000),
    school_year  varchar(9)   constraint nn_course_school_year not null,
    semester     smallint     constraint nn_course_semester    not null,
    units        smallint     constraint nn_course_units       not null,
    instructor   varchar(80),

    constraint pk_course primary key (course_id),

    -- No two courses share a code (e.g. only one DBMWEB1).
    constraint uq_course_code unique (code),

    -- Capital letters, optional space or hyphen, digits, optional letter:
    -- DBMWEB1, IT 101, CC-102, PROG2A
    constraint ck_course_code check (code ~ '^[A-Z]{2,10}[ -]?[0-9]{1,4}[A-Z]?$'),

    -- 1 = 1st semester, 2 = 2nd semester, 3 = summer
    constraint ck_course_semester check (semester in (1, 2, 3)),

    constraint ck_course_units check (units between 1 and 6),

    -- Looks like 2026-2027 AND the second year is the first year + 1.
    -- (CASE makes sure the ::int casts only run on text that is all digits.)
    constraint ck_course_school_year check (
        case
            when school_year ~ '^[0-9]{4}-[0-9]{4}$'
                then right(school_year, 4)::int = left(school_year, 4)::int + 1
            else false
        end
    )
);


-- ---------------------------------------------------------------------
--  PROJECT — something I built in a course.
--  Every project MUST belong to exactly one course (NOT NULL FK).
-- ---------------------------------------------------------------------
create table portfolio.project (
    project_id   integer      generated always as identity,
    course_id    integer      constraint nn_project_course_id  not null,
    title        varchar(120) constraint nn_project_title      not null,
    description  varchar(2000),
    tools        varchar(200),               -- e.g. "C#, ASP.NET Core MVC, Supabase"
    video_url    varchar(200),               -- YouTube link, optional
    repo_url     varchar(300),               -- source code link, optional
    created_at   timestamptz  constraint nn_project_created_at not null default now(),

    constraint pk_project primary key (project_id),

    -- A course with projects cannot be deleted (RESTRICT → error 23503).
    -- If a course_id ever changes, its projects follow (CASCADE).
    constraint fk_project_course foreign key (course_id)
        references portfolio.course (course_id)
        on delete restrict
        on update cascade,

    -- The same course cannot list two projects with the same title.
    constraint uq_project_course_title unique (course_id, title),

    -- youtube.com/watch?v=ID, youtube.com/shorts/ID or youtu.be/ID (ID = 11 chars)
    constraint ck_project_video_url check (
        video_url is null or
        video_url ~ '^https://(www\.|m\.)?(youtube\.com/(watch\?(\S*&)?v=|shorts/)|youtu\.be/)[A-Za-z0-9_-]{11}([?&#]\S*)?$'
    ),

    constraint ck_project_repo_url check (
        repo_url is null or repo_url ~ '^https://\S+$'
    )
);


-- ---------------------------------------------------------------------
--  PROJECT_IMAGE — a screenshot of a project (a project has many).
--  Stored as a PUBLIC URL, never as a file.
-- ---------------------------------------------------------------------
create table portfolio.project_image (
    image_id     integer      generated always as identity,
    project_id   integer      constraint nn_project_image_project_id not null,
    image_url    varchar(500) constraint nn_project_image_url        not null,
    caption      varchar(150),
    sort_order   smallint     constraint nn_project_image_sort_order not null default 1,

    constraint pk_project_image primary key (image_id),

    -- Images mean nothing without their project: deleting the project
    -- deletes its images too (CASCADE).
    constraint fk_project_image_project foreign key (project_id)
        references portfolio.project (project_id)
        on delete cascade
        on update cascade,

    -- The same URL cannot appear twice in one project (error 23505).
    -- This is what rolls back a project whose image list repeats a URL.
    constraint uq_project_image_url unique (project_id, image_url),

    -- Public https link ending in an image extension (query string allowed).
    constraint ck_project_image_url check (
        image_url ~* '^https://\S+\.(png|jpe?g|gif|webp|svg)(\?\S*)?$'
    ),

    constraint ck_project_image_sort_order check (sort_order >= 1)
);

-- Speeds up "all projects of a course" and "all images of a project".
create index ix_project_course_id       on portfolio.project (course_id);
create index ix_project_image_project_id on portfolio.project_image (project_id);
