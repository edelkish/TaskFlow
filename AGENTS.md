# TaskFlow - Instrucciones del Proyecto

## Metodología

- **Siempre usar BMAD** (Breakthrough Method for Agile AI Driven Development) para todo el trabajo de desarrollo en este repositorio.

## Agentes a usar

Durante el flujo de trabajo se deben usar las siguientes personas de BMAD según corresponda:

- **Architect** (`bmad-agent-architect`) - decisiones de arquitectura
- **UI Designer** (`bmad-agent-ux-designer`) - experiencia de usuario y diseño de interfaz
- **Analyst** (`bmad-agent-analyst`) - análisis de requisitos y dominio
- **Dev** (`bmad-agent-dev`) - implementación de código
- **QA** (`bmad-qa-generate-e2e-tests`) - verificación de calidad y generación de pruebas

## Arquitectura y principios

- El proyecto emplea **Arquitectura Limpia (Clean Architecture)** con capas separadas:
  - `TaskFlow.Domain` - entidades, enums, interfaces y excepciones de dominio
  - `TaskFlow.Application` - DTOs, servicios, validadores y mapeos de aplicación
  - `TaskFlow.Infrastructure` - data access, identity, repositorios y persistencia
  - `TaskFlow.Api` y `TaskFlow.Blazor` - presentación y API
- Todo el código debe cumplir los **principios SOLID**.
- Seguir las convenciones y la estructura definida en `TaskFlow-Architecture.md`.

## Diseño / Plantilla HTML

- Plantilla base del proyecto: `C:\wamp64\www\AdminLTE`
- Página de referencia: `starter.html`
- El UI debe basarse en la estructura, componentes y estilos de la plantilla AdminLTE.