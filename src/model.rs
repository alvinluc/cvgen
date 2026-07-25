use std::fs;
use std::path::Path;

use serde::Deserialize;

/// A parsed TOML profile. Every field is optional so a partially filled
/// profile still renders; missing values simply produce no output.
#[derive(Debug, Default, Deserialize)]
#[serde(default)]
pub struct Document {
    #[serde(rename = "type")]
    pub doc_type: Option<String>,
    pub name: String,
    pub headline: String,
    pub summary: String,
    pub date: String,
    pub subject: String,
    pub salutation: String,
    pub sign_off: String,
    pub recipient: Vec<String>,
    pub body: Vec<String>,
    pub contact: Vec<Contact>,
    pub experience: Vec<Experience>,
    pub skills: Vec<SkillGroup>,
    pub education: Vec<Education>,
    pub additional: Vec<Group>,
}

#[derive(Debug, Default, Deserialize)]
#[serde(default)]
pub struct Contact {
    pub label: String,
    pub value: String,
    pub url: String,
}

#[derive(Debug, Default, Deserialize)]
#[serde(default)]
pub struct Experience {
    pub role: String,
    pub company: String,
    pub dates: String,
    pub location: String,
    pub summary: String,
    pub highlights: Vec<String>,
    pub technologies: Vec<String>,
    pub progression: Vec<Progression>,
}

/// An earlier role at the same company, rendered under its parent entry.
#[derive(Debug, Default, Deserialize)]
#[serde(default)]
pub struct Progression {
    pub role: String,
    pub dates: String,
    pub location: String,
    pub summary: String,
    pub highlights: Vec<String>,
}

#[derive(Debug, Default, Deserialize)]
#[serde(default)]
pub struct SkillGroup {
    pub name: String,
    pub items: Vec<String>,
}

#[derive(Debug, Default, Deserialize)]
#[serde(default)]
pub struct Education {
    pub name: String,
    pub institution: String,
    pub dates: String,
    pub detail: String,
}

#[derive(Debug, Default, Deserialize)]
#[serde(default)]
pub struct Group {
    pub name: Option<String>,
    pub items: Vec<String>,
}

impl Document {
    pub fn is_cover_letter(&self) -> bool {
        self.doc_type.as_deref().unwrap_or("cv") == "cover-letter"
    }

    pub fn contact_line(&self) -> String {
        let parts: Vec<String> = self
            .contact
            .iter()
            .filter_map(|item| {
                let label = item.label.trim();
                let value = item.value.trim();
                if value.is_empty() {
                    return None;
                }
                Some(if label.is_empty() {
                    value.to_string()
                } else {
                    format!("{label}: {value}")
                })
            })
            .collect();
        parts.join("  |  ")
    }
}

pub fn load_toml(path: &Path) -> Result<Document, String> {
    let text = fs::read_to_string(path)
        .map_err(|error| format!("Input file not found: {} ({error})", path.display()))?;
    toml::from_str(&text).map_err(|error| format!("Invalid TOML in {}: {error}", path.display()))
}

/// Trim each value and drop the ones left empty.
pub fn clean_lines<S: AsRef<str>>(values: &[S]) -> Vec<&str> {
    values
        .iter()
        .map(|value| value.as_ref().trim())
        .filter(|value| !value.is_empty())
        .collect()
}

pub fn join_items<S: AsRef<str>>(values: &[S]) -> String {
    clean_lines(values).join(", ")
}

/// Join non-empty parts with a separator, e.g. dates and location.
pub fn join_parts<S: AsRef<str>>(parts: &[S], separator: &str) -> String {
    parts
        .iter()
        .map(|part| part.as_ref().trim())
        .filter(|part| !part.is_empty())
        .collect::<Vec<_>>()
        .join(separator)
}

#[cfg(test)]
mod tests {
    use super::*;

    fn document(toml: &str) -> Document {
        toml::from_str(toml).expect("valid TOML")
    }

    #[test]
    fn defaults_to_cv_when_type_is_missing() {
        assert!(!document("name = \"Ada\"").is_cover_letter());
        assert!(document("type = \"cover-letter\"").is_cover_letter());
    }

    #[test]
    fn unknown_keys_are_ignored() {
        let parsed = document("name = \"Ada\"\nnickname = \"Countess\"");
        assert_eq!(parsed.name, "Ada");
    }

    #[test]
    fn contact_line_skips_entries_without_a_value() {
        let parsed = document(
            r#"
            [[contact]]
            label = "Email"
            value = "ada@example.com"

            [[contact]]
            label = "Fax"
            value = "  "

            [[contact]]
            value = "London"
            "#,
        );
        assert_eq!(parsed.contact_line(), "Email: ada@example.com  |  London");
    }

    #[test]
    fn helpers_trim_and_drop_empty_values() {
        let values = vec![" one ".to_string(), String::new(), "two".to_string()];
        assert_eq!(clean_lines(&values), vec!["one", "two"]);
        assert_eq!(join_items(&values), "one, two");
        assert_eq!(join_parts(&["2019", "  "], " · "), "2019");
        assert_eq!(join_parts(&["2019", "Remote"], " · "), "2019 · Remote");
    }
}
