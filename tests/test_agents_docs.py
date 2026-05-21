"""
Tests for agent documentation files added in this PR:
  - AGENTS.md
  - docs/agents/domain.md
  - docs/agents/issue-tracker.md
  - docs/agents/triage-labels.md

These tests validate the structure, required sections, cross-references, and content
constraints specified in and for the agent skill documentation.
"""

import re
import unittest
from pathlib import Path

REPO_ROOT = Path(__file__).parent.parent


def read(relative_path: str) -> str:
    return (REPO_ROOT / relative_path).read_text(encoding="utf-8")


def headings(text: str, level: int) -> list[str]:
    """Return all heading texts at the given markdown heading level (1-based)."""
    prefix = "#" * level + " "
    result = []
    for line in text.splitlines():
        if line.startswith(prefix) and (
            len(line) == len(prefix) or line[len(prefix)] != "#"
        ):
            result.append(line[len(prefix):].strip())
    return result


# ---------------------------------------------------------------------------
# AGENTS.md
# ---------------------------------------------------------------------------

class TestAgentsMd(unittest.TestCase):
    """Validates AGENTS.md structure, content, and cross-references."""

    def setUp(self):
        self.path = REPO_ROOT / "AGENTS.md"
        self.text = read("AGENTS.md")

    def test_file_exists(self):
        self.assertTrue(self.path.exists(), "AGENTS.md must exist at the repo root")

    def test_h1_title(self):
        h1s = headings(self.text, 1)
        self.assertEqual(len(h1s), 1, "AGENTS.md should have exactly one H1")
        self.assertIn("Dread Mod", h1s[0])
        self.assertIn("Agent Instructions", h1s[0])

    def test_required_h2_sections_present(self):
        required = [
            "Build Output",
            "Thunderstore Package Requirements",
            "Versioning Rules",
            "Changelog Rules",
            "GitHub Workflow",
            "Agent skills",
        ]
        h2s = headings(self.text, 2)
        for section in required:
            self.assertIn(
                section,
                h2s,
                f"AGENTS.md is missing required H2 section: '{section}'",
            )

    def test_agent_skill_subsections_present(self):
        """The 'Agent skills' section must have three H3 subsections."""
        h3s = headings(self.text, 3)
        for subsection in ("Issue tracker", "Triage labels", "Domain docs"):
            self.assertIn(
                subsection,
                h3s,
                f"AGENTS.md Agent skills is missing H3 subsection: '{subsection}'",
            )

    def test_references_issue_tracker_doc(self):
        self.assertIn(
            "docs/agents/issue-tracker.md",
            self.text,
            "AGENTS.md must reference docs/agents/issue-tracker.md",
        )

    def test_references_triage_labels_doc(self):
        self.assertIn(
            "docs/agents/triage-labels.md",
            self.text,
            "AGENTS.md must reference docs/agents/triage-labels.md",
        )

    def test_references_domain_doc(self):
        self.assertIn(
            "docs/agents/domain.md",
            self.text,
            "AGENTS.md must reference docs/agents/domain.md",
        )

    def test_all_five_triage_labels_named(self):
        """Agent skills section must name all five canonical triage labels."""
        for label in (
            "needs-triage",
            "needs-info",
            "ready-for-agent",
            "ready-for-human",
            "wontfix",
        ):
            self.assertIn(
                label,
                self.text,
                f"AGENTS.md must mention triage label '{label}'",
            )

    def test_thunderstore_required_files_listed(self):
        """The Thunderstore table must list all five required package files."""
        for required_file in (
            "icon.png",
            "manifest.json",
            "README.md",
            "Dread.dll",
            "audio/",
        ):
            self.assertIn(
                required_file,
                self.text,
                f"Thunderstore requirements must mention '{required_file}'",
            )

    def test_manifest_json_example_has_required_fields(self):
        """The manifest.json code block must contain all required Thunderstore fields."""
        for field in ("name", "version_number", "website_url", "description", "dependencies"):
            self.assertIn(
                f'"{field}"',
                self.text,
                f"manifest.json example must contain field '{field}'",
            )

    def test_manifest_name_is_dread(self):
        self.assertIn('"name": "Dread"', self.text)

    def test_versioning_section_mentions_semantic_versioning(self):
        # The versioning rules section must mention semantic versioning format
        self.assertIn("MAJOR.MINOR.PATCH", self.text)

    def test_versioning_mentions_both_files_to_update(self):
        """Versioning rules must name both manifest.json and Plugin.cs."""
        self.assertIn("manifest.json", self.text)
        self.assertIn("Plugin.cs", self.text)

    def test_changelog_rule_no_em_dash_present(self):
        """The 'no em dash' rule text must appear in AGENTS.md."""
        # The rule must be documented
        self.assertIn("em dash", self.text, "Changelog rules must mention the em dash prohibition")

    def test_changelog_section_required_entry_fields(self):
        """Changelog rules must specify the four entry categories."""
        for category in ("Added", "Changed", "Fixed", "Removed"):
            self.assertIn(
                category,
                self.text,
                f"Changelog rules must list entry category '{category}'",
            )

    def test_github_remote_url_present(self):
        self.assertIn(
            "https://github.com/grompen91-droid/dreadREPO.git",
            self.text,
        )

    def test_github_branch_is_master(self):
        self.assertIn("master", self.text)

    def test_build_script_reference(self):
        self.assertIn("build.ps1", self.text)

    def test_dist_folder_reference(self):
        self.assertIn("dist", self.text)

    def test_no_true_em_dash_character(self):
        """AGENTS.md itself must not contain the Unicode em dash character U+2014."""
        self.assertNotIn(
            "\u2014",
            self.text,
            "AGENTS.md must not contain a true Unicode em dash character (U+2014)",
        )

    def test_section_count_not_reduced(self):
        """Guard against accidental section deletion: at least 6 H2 sections."""
        self.assertGreaterEqual(
            len(headings(self.text, 2)),
            6,
            "AGENTS.md must have at least 6 H2 sections",
        )

    def test_beInEx_dependency_in_manifest_example(self):
        self.assertIn("BepInEx-BepInExPack", self.text)

    # Boundary / regression
    def test_file_is_not_empty(self):
        self.assertGreater(len(self.text.strip()), 0)

    def test_file_ends_with_newline(self):
        raw = (REPO_ROOT / "AGENTS.md").read_bytes()
        self.assertEqual(raw[-1:], b"\n", "AGENTS.md must end with a newline")


# ---------------------------------------------------------------------------
# docs/agents/domain.md
# ---------------------------------------------------------------------------

class TestDomainMd(unittest.TestCase):
    """Validates docs/agents/domain.md structure and content."""

    def setUp(self):
        self.path = REPO_ROOT / "docs" / "agents" / "domain.md"
        self.text = read("docs/agents/domain.md")

    def test_file_exists(self):
        self.assertTrue(self.path.exists(), "docs/agents/domain.md must exist")

    def test_h1_title(self):
        h1s = headings(self.text, 1)
        self.assertEqual(len(h1s), 1)
        self.assertEqual(h1s[0], "Domain Docs")

    def test_required_h2_sections(self):
        required = [
            "Before exploring, read these",
            "File structure",
            "Use the glossary's vocabulary",
            "Flag ADR conflicts",
        ]
        h2s = headings(self.text, 2)
        for section in required:
            self.assertIn(
                section,
                h2s,
                f"docs/agents/domain.md missing H2: '{section}'",
            )

    def test_context_md_referenced(self):
        self.assertIn("CONTEXT.md", self.text)

    def test_context_map_md_referenced(self):
        self.assertIn("CONTEXT-MAP.md", self.text)

    def test_adr_directory_referenced(self):
        self.assertIn("docs/adr/", self.text)

    def test_grill_with_docs_skill_referenced(self):
        self.assertIn("/grill-with-docs", self.text)

    def test_silent_on_missing_files(self):
        """The doc must instruct to proceed silently when files don't exist."""
        self.assertIn("proceed silently", self.text)

    def test_single_context_structure_described(self):
        self.assertIn("Single-context", self.text)

    def test_multi_context_structure_described(self):
        self.assertIn("Multi-context", self.text)

    def test_adr_conflict_instruction_present(self):
        """ADR conflict flagging instruction must be present."""
        # The doc says to surface contradictions explicitly
        self.assertIn("Contradicts ADR", self.text)

    def test_glossary_vocabulary_instruction(self):
        """Instructions about using glossary vocabulary must be present."""
        self.assertIn("CONTEXT.md", self.text)
        # Must mention the glossary/vocabulary concept
        self.assertIn("glossary", self.text)

    def test_no_true_em_dash_character(self):
        self.assertNotIn(
            "\u2014",
            self.text,
            "docs/agents/domain.md must not contain a Unicode em dash (U+2014)",
        )

    def test_file_is_not_empty(self):
        self.assertGreater(len(self.text.strip()), 0)

    def test_file_ends_with_newline(self):
        raw = self.path.read_bytes()
        self.assertEqual(raw[-1:], b"\n")

    # Boundary / regression
    def test_src_context_adr_path_shown_in_multi_context(self):
        """Multi-context tree must show src/<context>/docs/adr/ pattern."""
        self.assertIn("src/", self.text)


# ---------------------------------------------------------------------------
# docs/agents/issue-tracker.md
# ---------------------------------------------------------------------------

class TestIssueTrackerMd(unittest.TestCase):
    """Validates docs/agents/issue-tracker.md content and CLI conventions."""

    def setUp(self):
        self.path = REPO_ROOT / "docs" / "agents" / "issue-tracker.md"
        self.text = read("docs/agents/issue-tracker.md")

    def test_file_exists(self):
        self.assertTrue(self.path.exists())

    def test_h1_title(self):
        h1s = headings(self.text, 1)
        self.assertEqual(len(h1s), 1)
        self.assertIn("Issue tracker", h1s[0])
        self.assertIn("GitHub", h1s[0])

    def test_conventions_section_present(self):
        self.assertIn("Conventions", headings(self.text, 2))

    def test_repository_slug_present(self):
        self.assertIn("grompen91-droid/dreadREPO", self.text)

    def test_github_repo_url_present(self):
        self.assertIn("https://github.com/grompen91-droid/dreadREPO", self.text)

    def test_gh_create_command_present(self):
        self.assertIn("gh issue create", self.text)

    def test_gh_view_command_present(self):
        self.assertIn("gh issue view", self.text)

    def test_gh_list_command_present(self):
        self.assertIn("gh issue list", self.text)

    def test_gh_comment_command_present(self):
        self.assertIn("gh issue comment", self.text)

    def test_gh_edit_label_command_present(self):
        self.assertIn("gh issue edit", self.text)

    def test_gh_close_command_present(self):
        self.assertIn("gh issue close", self.text)

    def test_all_six_operations_covered(self):
        """All six issue operations (create, view, list, comment, edit, close) must appear."""
        for op in ("create", "view", "list", "comment", "edit", "close"):
            self.assertIn(
                f"gh issue {op}",
                self.text,
                f"gh issue {op} must be documented",
            )

    def test_publish_skill_phrase_present(self):
        """Must clarify what 'publish to the issue tracker' means."""
        self.assertIn("publish to the issue tracker", self.text)

    def test_fetch_ticket_phrase_present(self):
        """Must clarify what 'fetch the relevant ticket' means."""
        self.assertIn("fetch the relevant ticket", self.text)

    def test_git_remote_inference_mentioned(self):
        """Must mention that gh infers the repo from git remote."""
        self.assertIn("git remote", self.text)

    def test_jq_filter_for_list_command(self):
        """The list command example must use jq for output filtering."""
        self.assertIn("jq", self.text)

    def test_heredoc_mentioned_for_multi_line(self):
        self.assertIn("heredoc", self.text)

    def test_no_true_em_dash_character(self):
        self.assertNotIn("\u2014", self.text)

    def test_file_is_not_empty(self):
        self.assertGreater(len(self.text.strip()), 0)

    def test_file_ends_with_newline(self):
        raw = self.path.read_bytes()
        self.assertEqual(raw[-1:], b"\n")

    # Boundary / regression: label operations documented with both add and remove
    def test_add_label_flag_present(self):
        self.assertIn("--add-label", self.text)

    def test_remove_label_flag_present(self):
        self.assertIn("--remove-label", self.text)

    def test_list_command_includes_json_output(self):
        """The list command should use --json for structured output."""
        self.assertIn("--json", self.text)


# ---------------------------------------------------------------------------
# docs/agents/triage-labels.md
# ---------------------------------------------------------------------------

class TestTriageLabelsMd(unittest.TestCase):
    """Validates docs/agents/triage-labels.md label table and content."""

    CANONICAL_LABELS = [
        "needs-triage",
        "needs-info",
        "ready-for-agent",
        "ready-for-human",
        "wontfix",
    ]

    def setUp(self):
        self.path = REPO_ROOT / "docs" / "agents" / "triage-labels.md"
        self.text = read("docs/agents/triage-labels.md")

    def test_file_exists(self):
        self.assertTrue(self.path.exists())

    def test_h1_title(self):
        h1s = headings(self.text, 1)
        self.assertEqual(len(h1s), 1)
        self.assertIn("Triage Labels", h1s[0])

    def test_all_five_canonical_labels_present(self):
        for label in self.CANONICAL_LABELS:
            self.assertIn(
                label,
                self.text,
                f"Triage label '{label}' must appear in triage-labels.md",
            )

    def test_exactly_five_data_rows_in_table(self):
        """The label table must have exactly five data rows (one per label)."""
        # Data rows start with '|' and are not separator rows (---) or header rows
        separator_pattern = re.compile(r"^\|\s*-+\s*\|")
        header_keywords = {"Label in mattpocock", "Label in our tracker", "Meaning"}
        data_rows = []
        for line in self.text.splitlines():
            stripped = line.strip()
            if not stripped.startswith("|"):
                continue
            if separator_pattern.match(stripped):
                continue
            if any(kw in stripped for kw in header_keywords):
                continue
            data_rows.append(stripped)
        self.assertEqual(
            len(data_rows),
            5,
            f"Expected 5 data rows in triage table, found {len(data_rows)}: {data_rows}",
        )

    def test_table_has_three_columns(self):
        """Each table data row must have three pipe-delimited columns."""
        separator_pattern = re.compile(r"^\|\s*-+\s*\|")
        header_keywords = {"Label in mattpocock", "Label in our tracker", "Meaning"}
        for line in self.text.splitlines():
            stripped = line.strip()
            if not stripped.startswith("|"):
                continue
            if separator_pattern.match(stripped):
                continue
            if any(kw in stripped for kw in header_keywords):
                continue
            # Count non-empty cells
            cells = [c.strip() for c in stripped.strip("|").split("|")]
            self.assertEqual(
                len(cells),
                3,
                f"Expected 3 columns in row '{stripped}', got {len(cells)}",
            )

    def test_skill_labels_match_tracker_labels(self):
        """For this repo the skill label and tracker label are identical; verify parity."""
        # Extract rows and check that column 1 == column 2 (backtick-wrapped)
        separator_pattern = re.compile(r"^\|\s*-+\s*\|")
        header_keywords = {"Label in mattpocock", "Label in our tracker", "Meaning"}
        for line in self.text.splitlines():
            stripped = line.strip()
            if not stripped.startswith("|"):
                continue
            if separator_pattern.match(stripped):
                continue
            if any(kw in stripped for kw in header_keywords):
                continue
            cells = [c.strip() for c in stripped.strip("|").split("|")]
            if len(cells) >= 2:
                # Both should contain the same label string (possibly in backticks)
                label_skill = cells[0].strip("`").strip()
                label_tracker = cells[1].strip("`").strip()
                self.assertEqual(
                    label_skill,
                    label_tracker,
                    f"Skill label '{label_skill}' does not match tracker label '{label_tracker}'",
                )

    def test_each_label_has_non_empty_meaning(self):
        """Every label row must have a non-empty meaning in the third column."""
        separator_pattern = re.compile(r"^\|\s*-+\s*\|")
        header_keywords = {"Label in mattpocock", "Label in our tracker", "Meaning"}
        for line in self.text.splitlines():
            stripped = line.strip()
            if not stripped.startswith("|"):
                continue
            if separator_pattern.match(stripped):
                continue
            if any(kw in stripped for kw in header_keywords):
                continue
            cells = [c.strip() for c in stripped.strip("|").split("|")]
            if len(cells) >= 3:
                self.assertGreater(
                    len(cells[2]),
                    0,
                    f"Label row has empty meaning: '{stripped}'",
                )

    def test_needs_triage_meaning(self):
        self.assertIn("Maintainer needs to evaluate", self.text)

    def test_needs_info_meaning(self):
        self.assertIn("Waiting on reporter", self.text)

    def test_ready_for_agent_meaning(self):
        self.assertIn("ready for an AFK agent", self.text)

    def test_ready_for_human_meaning(self):
        self.assertIn("human implementation", self.text)

    def test_wontfix_meaning(self):
        self.assertIn("Will not be actioned", self.text)

    def test_edit_instruction_present(self):
        """The file must instruct users to edit the right-hand column when customising."""
        self.assertIn("Edit the right-hand column", self.text)

    def test_role_application_instruction_present(self):
        """The file must explain how to map roles to label strings."""
        self.assertIn("use the corresponding label string", self.text)

    def test_no_true_em_dash_character(self):
        self.assertNotIn("\u2014", self.text)

    def test_file_is_not_empty(self):
        self.assertGreater(len(self.text.strip()), 0)

    def test_file_ends_with_newline(self):
        raw = self.path.read_bytes()
        self.assertEqual(raw[-1:], b"\n")

    # Boundary / regression
    def test_needs_triage_is_first_label(self):
        """needs-triage is the default starting label and should appear first."""
        idx_triage = self.text.find("needs-triage")
        idx_info = self.text.find("needs-info")
        self.assertGreater(
            idx_info,
            idx_triage,
            "'needs-triage' should appear before 'needs-info' in the table",
        )

    def test_wontfix_is_last_label(self):
        """wontfix is the terminal label and should appear last."""
        idx_wontfix = self.text.rfind("wontfix")
        for label in ("needs-triage", "needs-info", "ready-for-agent", "ready-for-human"):
            idx = self.text.rfind(label)
            self.assertGreater(
                idx_wontfix,
                idx,
                f"'wontfix' should appear after '{label}'",
            )


# ---------------------------------------------------------------------------
# Cross-file consistency
# ---------------------------------------------------------------------------

class TestCrossFileConsistency(unittest.TestCase):
    """Validates consistency guarantees across all four agent doc files."""

    def setUp(self):
        self.agents_md = read("AGENTS.md")
        self.triage_md = read("docs/agents/triage-labels.md")
        self.issue_md = read("docs/agents/issue-tracker.md")
        self.domain_md = read("docs/agents/domain.md")

    def test_triage_labels_in_agents_match_triage_doc(self):
        """All five labels named in AGENTS.md must also appear in triage-labels.md."""
        for label in (
            "needs-triage",
            "needs-info",
            "ready-for-agent",
            "ready-for-human",
            "wontfix",
        ):
            self.assertIn(label, self.triage_md, f"'{label}' missing from triage-labels.md")
            self.assertIn(label, self.agents_md, f"'{label}' missing from AGENTS.md")

    def test_repository_slug_consistent_across_files(self):
        """The repo slug must be the same in AGENTS.md and issue-tracker.md."""
        slug = "grompen91-droid/dreadREPO"
        self.assertIn(slug, self.agents_md)
        self.assertIn(slug, self.issue_md)

    def test_all_three_agent_doc_files_referenced_from_agents_md(self):
        for doc in (
            "docs/agents/issue-tracker.md",
            "docs/agents/triage-labels.md",
            "docs/agents/domain.md",
        ):
            self.assertIn(
                doc,
                self.agents_md,
                f"AGENTS.md must reference '{doc}'",
            )

    def test_none_of_the_docs_contain_unicode_em_dash(self):
        """No added documentation file may contain the Unicode em dash U+2014."""
        files = {
            "AGENTS.md": self.agents_md,
            "docs/agents/domain.md": self.domain_md,
            "docs/agents/issue-tracker.md": self.issue_md,
            "docs/agents/triage-labels.md": self.triage_md,
        }
        for name, content in files.items():
            self.assertNotIn(
                "\u2014",
                content,
                f"'{name}' contains a Unicode em dash (U+2014)",
            )

    def test_issue_tracker_file_count_h2(self):
        """issue-tracker.md must have at least 2 H2 sections (Conventions + two When clauses)."""
        self.assertGreaterEqual(len(headings(self.issue_md, 2)), 2)

    def test_domain_doc_has_at_least_four_h2_sections(self):
        self.assertGreaterEqual(len(headings(self.domain_md, 2)), 4)


if __name__ == "__main__":
    unittest.main()
