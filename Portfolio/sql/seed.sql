-- =====================================================================
--  PORTFOLIO  ·  seed.sql
--  Sample rows so the viewer has something to show. Run AFTER schema.sql.
--
--  TODO before presenting: replace the courses, descriptions and
--  placeholder images below with your REAL courses, screenshots
--  (public Supabase Storage or GitHub URLs) and YouTube demo links.
--  You can also do it from the Editor screens instead.
-- =====================================================================

-- Start clean (images and projects first because of the foreign keys).
delete from portfolio.project_image;
delete from portfolio.project;
delete from portfolio.course;

-- ---------------------------------------------------------------------
--  COURSES
-- ---------------------------------------------------------------------
insert into portfolio.course (code, title, description, school_year, semester, units, instructor) values
('DBMWEB1', 'Database Management and Web Systems',
 'Relational design, SQL, transactions and ASP.NET Core MVC web applications on PostgreSQL (Supabase).',
 '2026-2027', 1, 3, 'Khmer S. Motal'),

-- TODO: change to the real course where you built the health monitoring system
('WEBSYS1', 'Web Systems and Technologies',
 'Server-side web development with PHP and MySQL.',
 '2026-2027', 1, 3, null),

-- TODO: change to the real course where you built the chatbot
('ITELEC1', 'IT Elective 1',
 'Applied AI and intelligent systems.',
 '2026-2027', 1, 3, null),

-- A course with no projects yet (a course MAY have no projects).
-- TODO: change or delete
('PROG2', 'Computer Programming 2',
 'Object-oriented programming in C#.',
 '2025-2026', 2, 3, null);


-- ---------------------------------------------------------------------
--  PROJECTS  (course_id looked up by the unique course code)
-- ---------------------------------------------------------------------
insert into portfolio.project (course_id, title, description, tools, video_url, repo_url) values
((select course_id from portfolio.course where code = 'DBMWEB1'),
 'Library System',
 'Catalog of books and members with borrowing and returning. Borrowing runs in one transaction so a loan and the book''s status are saved together or not at all.',
 'C#, ASP.NET Core MVC, Npgsql, Supabase (PostgreSQL)',
 null, null),

((select course_id from portfolio.course where code = 'DBMWEB1'),
 'Portfolio',
 'This site. Courses, projects, images and videos stored in a Supabase schema, with a viewer side and a signed-in editor side.',
 'C#, ASP.NET Core MVC, Npgsql, Supabase (PostgreSQL), Bootstrap',
 null, null),

((select course_id from portfolio.course where code = 'WEBSYS1'),
 'Barangay Health Services and Community Health Monitoring System',
 'Group project: resident registration, consultations, vaccination, maternal and child health, medicine inventory, appointments and reports.',
 'PHP, MySQL, XAMPP/Apache, HTML, CSS, JavaScript',
 null, null),

((select course_id from portfolio.course where code = 'ITELEC1'),
 'Barangay Services and Ordinance Chatbot',
 'Group project: a retrieval-augmented (RAG) chatbot that answers questions about barangay government services and ordinances.',
 'Python, RAG, vector search',
 null, null);


-- ---------------------------------------------------------------------
--  IMAGES  (placeholders — replace with your own screenshots)
-- ---------------------------------------------------------------------
insert into portfolio.project_image (project_id, image_url, caption, sort_order) values
((select project_id from portfolio.project where title = 'Library System'),
 'https://placehold.co/1200x750.png?text=Library+System+-+Catalog', 'Book catalog', 1),
((select project_id from portfolio.project where title = 'Library System'),
 'https://placehold.co/1200x750.png?text=Library+System+-+Borrow', 'Borrowing a book', 2),

((select project_id from portfolio.project where title = 'Portfolio'),
 'https://placehold.co/1200x750.png?text=Portfolio+-+Viewer', 'Viewer side', 1),
((select project_id from portfolio.project where title = 'Portfolio'),
 'https://placehold.co/1200x750.png?text=Portfolio+-+Editor', 'Editor side', 2),

((select project_id from portfolio.project where title = 'Barangay Health Services and Community Health Monitoring System'),
 'https://placehold.co/1200x750.png?text=Health+Monitoring+-+Dashboard', 'Dashboard', 1),

((select project_id from portfolio.project where title = 'Barangay Services and Ordinance Chatbot'),
 'https://placehold.co/1200x750.png?text=Ordinance+Chatbot', 'Chat screen', 1);
