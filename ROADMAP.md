# Surgewave Bootcamp Roadmap

## Current Status

36 units, ~200 lessons, 8 learning paths, 35 guided tours, Knowledge Assessments per unit.

---

## Planned

### SCORM Export (LMS Integration)

Export the Bootcamp as SCORM 1.2 packages for Learning Management Systems (Moodle, Canvas, iSpring, etc.).

| Feature | Description | Priority |
|---------|-------------|----------|
| Markdown → HTML Conversion | Convert lesson README.md to styled HTML with code syntax highlighting | High |
| Knowledge Assessments → Interactive Quizzes | Parse KNOWLEDGE_ASSESSMENT.md into graded quizzes (MC, True/False, code challenges) | High |
| imsmanifest.xml Generation | SCORM 1.2 manifest with SCO per lesson, sequencing per unit | High |
| SCORM RTE API | JavaScript wrapper for LMS communication (status, score, bookmarks) | High |
| Progress Tracking | cmi.core.lesson_status per lesson, cmi.core.score.raw per assessment | High |
| Per-Unit Packages | Export individual units as standalone SCORM packages (for modular LMS import) | Medium |
| Full Bootcamp Package | Single SCORM package with all 36 units | Medium |
| Code Playground | Embedded code editor for exercises (Monaco Editor or similar) | Low |
| Instructor Dashboard | Trainer view with learner progress across all units | Low |

**Requirements:**
- Pure PowerShell script (no external tools)
- SCORM 1.2 (wider LMS support than SCORM 2004)
- Offline-capable (no CDN dependencies in package)
- Pass/fail threshold: 70% on assessments
- Output: `artifacts/surgewave-bootcamp-scorm.zip`

### Content Improvements

| Feature | Description | Priority |
|---------|-------------|----------|
| Video Walkthroughs | Short video per unit demonstrating key concepts | Low |
| Instructor Guide | Teaching notes, timing suggestions, discussion prompts per unit | Low |
| Certification Exam | Final assessment covering all learning paths | Low |
