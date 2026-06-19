# DEC-002: Classic Input Manager vs. New Input System

- **Status:** APPROVED
- **Date:** 2026-06-16
- **Context:** Unity 6 defaults to the New Input System package, throwing errors when legacy `Input.GetAxis` calls are invoked.
- **Decision:** Explicitly changed player settings player preference to **Active Input Handling: Both**.
- **Tradeoff:** Allows lightning-fast testing using standard controller and keyboard bindings during Phase 2/3. Ripping this out and swapping to touch screen joysticks is pushed to Phase 5 (Mobile Pass) to optimize early phase developer momentum.
