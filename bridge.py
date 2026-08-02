"""Core bridge models for the Sky cognitive system.

This module defines the primary data containers used by the engine.
"""

from __future__ import annotations

from dataclasses import dataclass, field
from typing import Any


@dataclass(slots=True)
class Node:
    """Represents an entity in the cognitive graph."""

    identity: str
    state: dict[str, Any] = field(default_factory=dict)
    relationships: list[str] = field(default_factory=list)
    dependencies: list[str] = field(default_factory=list)
    history: list[str] = field(default_factory=list)


@dataclass(slots=True)
class Event:
    """Represents a meaningful change that may require adaptation."""

    change: str
    cause: str
    severity: float = 0.0
    uncertainty: float = 0.0


@dataclass(slots=True)
class Decision:
    """Represents a candidate action and the rationale around it."""

    options: list[str] = field(default_factory=list)
    predicted_outcomes: dict[str, str] = field(default_factory=dict)
    selected_action: str | None = None


@dataclass(slots=True)
class Memory:
    """Represents a stored result and the lesson learned from it."""

    result: str
    lesson_learned: str


def demo_cycle() -> tuple[Node, Event, Decision, Memory]:
    """Builds a minimal end-to-end example using all core models."""

    node = Node(
        identity="sky-core",
        state={"mode": "observe", "load": 0.34},
        relationships=["sensor-net", "planner"],
        dependencies=["memory-store", "event-bus"],
        history=["booted", "baseline-scan"],
    )

    event = Event(
        change="incoming workload spike",
        cause="new request burst",
        severity=0.72,
        uncertainty=0.15,
    )

    decision = Decision(
        options=["scale_workers", "defer_low_priority"],
        predicted_outcomes={
            "scale_workers": "stabilize latency quickly",
            "defer_low_priority": "reduce load with slower completion",
        },
        selected_action="scale_workers",
    )

    memory = Memory(
        result="latency remained below threshold",
        lesson_learned="early scaling is safer during burst patterns",
    )

    return node, event, decision, memory


if __name__ == "__main__":
    demo_node, demo_event, demo_decision, demo_memory = demo_cycle()
    print("Node:", demo_node)
    print("Event:", demo_event)
    print("Decision:", demo_decision)
    print("Memory:", demo_memory)