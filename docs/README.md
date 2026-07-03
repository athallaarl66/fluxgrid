# Documentation

This directory contains all project documentation organized by type.

## Structure

- **features/** - Product Requirements Documents (PRD) and Technical Design documents
- **production/** - Scenario-level breakdown files (prod, testing, design, tech)
- **test-reports/** - QA test reports and spec test documents

## Usage

Documentation is generated using the TVP-SDD-Dev CLI commands:
- `sdd-gen /prd <featureName>` - Generate PRD
- `sdd-gen /technical <featureName>` - Generate Technical Design
- `sdd-gen /sdd-breakdown-task <prd-file> [featureName]` - Generate scenario breakdowns
- `sdd-gen /spec-test <featureName>` - Generate Spec Test
- `sdd-gen /qa-report <featureName>` - Generate QA Report
