# portfolio

A web portfolio of the courses I have taken and the projects I made in them.
ASP.NET Core MVC · Npgsql · Supabase PostgreSQL (schema `portfolio`).

- **Viewer side** (anyone): courses, each course's projects, each project's screenshots and YouTube demo, plus search.
- **Editor side** (`/Editor`, sign-in required): add, edit and delete courses, projects and images.
- The link in the top bar (**Editor** / **View as visitor**) switches between the two.

---

## 1. Set up the database (Supabase)

1. Open your Supabase project. Free projects pause after 7 days without use, so open it before you present.
2. Go to **SQL Editor → New query**, paste `sql/schema.sql`, and click **Run**.
3. Open a new query, paste `sql/seed.sql`, and click **Run**.
4. Check **Table Editor → schema `portfolio`**. You should see `course`, `project` and `project_image`.

> `schema.sql` drops and recreates the tables. Run it again only if you want to start over.

## 2. Create the ASP.NET Core project

```bash
dotnet new mvc -n portfolio
cd portfolio
dotnet add package Npgsql
```

Then copy **everything in this folder** into the new `portfolio` folder, overwriting the template's files
(`Program.cs`, `Controllers/HomeController.cs`, `Views/Shared/_Layout.cshtml`, `wwwroot/css/site.css` and others).
Delete these two template files, which this project does not use:

- `Views/Home/Privacy.cshtml`
- `Views/Shared/_Layout.cshtml.css`

Keep the template's `portfolio.csproj`, `Properties/launchSettings.json`, `wwwroot/lib/` (Bootstrap, jQuery,
jQuery Validation) and `Views/Shared/_ValidationScriptsPartial.cshtml`.

## 3. Keep the secrets out of GitHub (user-secrets)

The connection string and the editor password are **not** in `appsettings.json`.
They are stored on your computer only:

```bash
dotnet user-secrets init
dotnet user-secrets set "ConnectionStrings:Portfolio" "Host=aws-0-ap-southeast-1.pooler.supabase.com;Port=5432;Database=postgres;Username=postgres.YOUR_PROJECT_REF;Password=YOUR_DB_PASSWORD;SSL Mode=Require"
dotnet user-secrets set "Editor:Password" "choose-a-strong-password"
```

Get the host and username from Supabase: **Connect → Connection string → Session pooler**. You can also reuse
the connection string from your Library System.

Put your name and headline in `appsettings.json` under `"Portfolio"`.

## 4. Run

```bash
dotnet run
```

Open the address it prints. To sign in to the editor, click **Editor** and enter the password you set above.

## 5. Publish to GitHub (public repository named `portfolio`)

```bash
git init
git add .
git commit -m "Portfolio: final lab exam"
git branch -M main
git remote add origin https://github.com/YOUR-GITHUB-USERNAME/portfolio.git
git push -u origin main
```

Before pushing, run `git grep -n "Password="`. Only the placeholder lines in `appsettings.json` and this README
should appear. Write the repository link in Part 3 of the exam paper.

## 6. Your own images and videos

- **Images:** in Supabase go to **Storage → New bucket**, name it `portfolio`, and turn **Public** ON. Upload your
  screenshots, then use **Get URL**. The link looks like
  `https://xxxx.supabase.co/storage/v1/object/public/portfolio/library-1.png`.
  You can also commit images to `wwwroot/img/` and use the `raw.githubusercontent.com/...` link.
  Google Drive, Facebook and OneDrive links do not display.
- **Videos:** upload a demo to YouTube (Unlisted is fine) and paste the link into the project form.
- Replace the placeholder courses and images from `seed.sql` with your real ones, either from the editor
  screens or by editing `seed.sql`.

---

## How the rubric is met

| Criterion | Where |
|---|---|
| **Database design and ERD** | `sql/schema.sql`: COURSE (unique `code`) 1-to-many PROJECT; PROJECT_IMAGE in its own table (a project has many); `video_url` on PROJECT; every constraint named (`pk_`, `fk_`, `uq_`, `ck_`, `nn_`); 3 UNIQUE and 8 CHECK constraints; delete rules RESTRICT and CASCADE. `sql/seed.sql`. Business rules and the ERD are filled in on the exam paper and in `docs/erd.png`. |
| **MVC with a data layer** | `Repositories/CourseRepository.cs`, `ProjectRepository.cs` and `ProjectImageRepository.cs`, one per table, registered in `Program.cs`. All SQL is in these files, none in controllers or views. Viewer (read-only): `Controllers/CoursesController.cs` and `ProjectsController.cs`. Editor: `Areas/Editor/Controllers/*`. The mode-switch link is in `_Layout.cshtml`. |
| **A project and its images in one transaction** | `ProjectRepository.AddWithImagesAsync` and `UpdateWithImagesAsync`: BEGIN, insert the project, insert each image on the same connection and transaction, then COMMIT. Any error triggers ROLLBACK and is rethrown. The textarea takes one URL per line. |
| **Validation** | Attributes on `Models/Course.cs`, `Models/ProjectImage.cs` and `ViewModels/ViewModels.cs` match the column sizes. Regular expressions are in `Validation/Patterns.cs`: image URL (`ImageUrlAttribute`, `Regex.IsMatch` line by line), YouTube link, course code, school year and repository link. The browser checks them via jQuery Validation plus `wwwroot/js/validation.js`, and the server checks them again with `ModelState.IsValid`. |
| **Error handling** | `Data/DbErrors.cs` maps 23503, 23505, 23514 and 23502 (by constraint name) to a message placed under the right box with `AddModelError`. A missing id returns `NotFound()`, which shows `Views/Home/Status.cshtml`. Anything unexpected shows `Views/Shared/Error.cshtml` (`UseExceptionHandler`). |
| **Creativity** | A "card catalog" design (course cards look like library catalog cards), a teal editor mode with a banner, search and course filter chips, a course timeline, a live image preview that flags broken and repeated URLs, and a lightbox gallery. |
| **Extra credit** | Cookie sign-in (`AccountController`). `[Authorize]` is on every editor controller. The password is in user-secrets, not the repository. |

---

## Oral presentation: demo script (about 10 minutes)

1. **Viewer.** Home (stats, latest projects, timeline) → Courses → a course → a project (screenshots, lightbox, video). Then search for "Supabase".
2. **Sign in.** Click **Editor**. Try `/Editor/Courses` in a private window first to show that it redirects to sign-in.
3. **Add a course.** Use `CC 999` or something similar.
   - **23505:** add another course with code `DBMWEB1`. The message appears under *Course code*.
   - **23514:** use school year `2025-2027`. The browser regex accepts it, but the database's `ck_course_school_year` refuses it. The message appears under *School year*.
4. **23503:** delete `DBMWEB1`. It still has projects, so `fk_project_course` (ON DELETE RESTRICT) refuses. The message appears on the delete page.
5. **Validation.** In the project form, type a Google Drive link as an image. You get "Line 1 is not a public image link" before the form is sent. Then type `https://vimeo.com/1` as the video link.
6. **Rollback.** Add a new project with 3 image URLs where line 3 repeats line 1. You get "Image 3 repeats image 1 … Nothing was saved." Open the Projects list: the project is **not** there, because the project insert was rolled back too. Remove the repeat and save; now it appears.
7. **404.** Open `/Projects/Details/99999`.
8. **Friendly error page.** Temporarily set a wrong password with
   `dotnet user-secrets set "ConnectionStrings:Portfolio" "...Password=wrong..."`, restart and reload. Then set it back.
9. **Sign out.** Then try an editor URL again; it is refused.

## Business rules (Part 3)

| # | ENTITY 1 | may / must | verb | number | ENTITY 2 |
|---|---|---|---|---|---|
| 1 | COURSE | may | have | many | PROJECT |
| 2 | PROJECT | must | belong to | one | COURSE |
| 3 | PROJECT | may | have | many | PROJECT_IMAGE |
| 4 | PROJECT_IMAGE | must | belong to | one | PROJECT |

"may" puts a circle beside the many side, so a course can have no projects yet. "must" puts a bar beside the one
side, which becomes `NOT NULL` on the foreign key. "many" puts a crow's foot beside the many side, so the foreign
key goes in that table.

## Explaining the design (likely questions)

- **Why is images a table, not a column?** A project has many images. A column holds one value, and a list in one
  column breaks first normal form. Each image row has its own key, URL, caption and order.
- **Why RESTRICT on course → project?** Deleting a course must not silently delete my work. The database refuses
  (23503) until the projects are moved or deleted.
- **Why CASCADE on project → image?** An image has no meaning without its project, so deleting the project deletes
  its images.
- **Why UNIQUE (project_id, image_url)?** The same screenshot twice in one project is a mistake. This constraint
  is also what makes the rollback demo work.
- **Why is project_count a dashed oval?** It is derived: `COUNT(*)` in `CourseRepository`, never stored, so it can
  never be out of date.
- **Why identity keys and also a unique code?** `course_id` never changes, so foreign keys stay stable. `code` is the
  natural key people use, so it is UNIQUE.
- **Where does the transaction happen?** In `ProjectRepository.AddWithImagesAsync`. One connection and one
  `BeginTransactionAsync`. `ProjectImageRepository.InsertAsync` receives the same `conn` and `tx`, then
  `CommitAsync`. In `catch`, `RollbackAsync` runs and the exception is rethrown. This is the same pattern as
  BorrowAsync in the Library System.
- **How does a database error reach the page?** The controller catches the `PostgresException` when its `SqlState`
  is one of the four codes. `DbErrors.FieldFor` picks the box from `ConstraintName`, `DbErrors.Message` writes the
  text, and `ModelState.AddModelError` puts it under that box.

## Project map

```
Program.cs                      services, repositories, sign-in, error pages, routes
Data/DbErrors.cs                SQLSTATE + constraint name → message and field
Data/DbHelpers.cs               nullable parameters, safe search
Repositories/                   one class per table (all SQL lives here)
Models/                         Course, Project, ProjectImage (+ validation attributes)
ViewModels/ViewModels.cs        project form, login, page models
Validation/Patterns.cs          the regular expressions
Validation/ImageUrlAttribute.cs line-by-line image URL check (server + browser)
Controllers/                    viewer side (read-only) + Account (sign in/out)
Areas/Editor/Controllers/       editor side: Home, Courses, Projects, Images
Views/, Areas/Editor/Views/     Razor views
wwwroot/css/site.css            the design
wwwroot/js/site.js              image preview, lightbox, upper-case codes
wwwroot/js/validation.js        browser side of [ImageUrl]
sql/schema.sql, sql/seed.sql    database
docs/erd.png, docs/erd.svg      ERD
```
