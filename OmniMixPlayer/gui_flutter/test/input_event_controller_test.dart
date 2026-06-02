import 'package:flutter_test/flutter_test.dart';
import 'package:omnimix_gui/models/input/input_event.dart';
import 'package:omnimix_gui/providers/input/input_event_controller.dart';

void main() {
  test('fires a multi-key binding once when all keys are pressed', () {
    final controller = InputEventController();
    var fireCount = 0;

    controller.registerBinding(
      InputBinding(
        id: 'combo',
        keys: {InputKeyId.gamepadButton('a'), InputKeyId.gamepadButton('b')},
      ),
      (_) => fireCount++,
    );

    controller.setKeyPressed(
      const InputKeyId.gamepadButton('a', deviceId: 'pad-1'),
      true,
    );
    expect(fireCount, 0);

    controller.setKeyPressed(
      const InputKeyId.gamepadButton('b', deviceId: 'pad-1'),
      true,
    );
    expect(fireCount, 1);

    controller.setKeyPressed(
      const InputKeyId.gamepadButton('b', deviceId: 'pad-1'),
      true,
    );
    expect(fireCount, 1);

    controller.dispose();
  });

  test('supports modifier bindings', () {
    final controller = InputEventController();
    var fired = false;

    controller.registerBinding(
      InputBinding(
        id: 'modifier-combo',
        keys: {InputKeyId.gamepadButton('start')},
        modifiers: {InputModifier.control},
      ),
      (_) => fired = true,
    );

    controller.setKeyPressed(const InputKeyId.keyboard('ControlLeft'), true);
    controller.setKeyPressed(
      const InputKeyId.gamepadButton('start', deviceId: 'pad-1'),
      true,
    );

    expect(fired, isTrue);
    controller.dispose();
  });

  test('fires release bindings when a matching chord stops matching', () {
    final controller = InputEventController();
    var releaseCount = 0;

    controller.registerBinding(
      InputBinding(
        id: 'release-combo',
        keys: {InputKeyId.gamepadButton('x'), InputKeyId.gamepadButton('y')},
        trigger: InputBindingTrigger.release,
      ),
      (_) => releaseCount++,
    );

    controller.setKeyPressed(
      const InputKeyId.gamepadButton('x', deviceId: 'pad-1'),
      true,
    );
    controller.setKeyPressed(
      const InputKeyId.gamepadButton('y', deviceId: 'pad-1'),
      true,
    );
    controller.setKeyPressed(
      const InputKeyId.gamepadButton('x', deviceId: 'pad-1'),
      false,
    );

    expect(releaseCount, 1);
    controller.dispose();
  });

  test(
    'custom shortcut binding triggers correctly for single and multiple keys',
    () async {
      final controller = InputEventController();
      var fireCount = 0;

      controller.registerShortcutAction(
        ShortcutAction(
          id: 'test_action',
          descriptionKey: 'testAction',
          onTrigger: () => fireCount++,
        ),
      );

      // Single key binding: A
      final bindingA = CustomShortcutBinding(
        actionId: 'test_action',
        regularKeys: [const InputKeyId.gamepadButton('a'), null, null, null],
      );
      await controller.saveCustomBinding(bindingA);

      controller.setKeyPressed(
        const InputKeyId.gamepadButton('a', deviceId: 'pad-1'),
        true,
      );
      expect(fireCount, 1);

      controller.setKeyPressed(
        const InputKeyId.gamepadButton('a', deviceId: 'pad-1'),
        false,
      );

      // Combo keys: A AND B
      final bindingAB = CustomShortcutBinding(
        actionId: 'test_action',
        regularKeys: [
          const InputKeyId.gamepadButton('a'),
          const InputKeyId.gamepadButton('b'),
          null,
          null,
        ],
        operators: ['and', 'and', 'and'],
      );
      await controller.saveCustomBinding(bindingAB);

      controller.setKeyPressed(
        const InputKeyId.gamepadButton('a', deviceId: 'pad-1'),
        true,
      );
      expect(fireCount, 1); // Not triggered yet

      controller.setKeyPressed(
        const InputKeyId.gamepadButton('b', deviceId: 'pad-1'),
        true,
      );
      expect(fireCount, 2); // Triggered!

      controller.dispose();
    },
  );

  test(
    'custom shortcut binding supports prefix key and negation shielding',
    () async {
      final controller = InputEventController();
      var fireCount = 0;

      controller.registerShortcutAction(
        ShortcutAction(
          id: 'test_action',
          descriptionKey: 'testAction',
          onTrigger: () => fireCount++,
        ),
      );

      // Prefix key P (start) and regular key K (a) - Normal prefix (must be pressed)
      final bindingPrefix = CustomShortcutBinding(
        actionId: 'test_action',
        prefixKey: const InputKeyId.gamepadButton('start'),
        prefixNegated: false,
        regularKeys: [const InputKeyId.gamepadButton('a'), null, null, null],
      );
      await controller.saveCustomBinding(bindingPrefix);

      // Press regular key alone
      controller.setKeyPressed(
        const InputKeyId.gamepadButton('a', deviceId: 'pad-1'),
        true,
      );
      expect(fireCount, 0); // No prefix, shouldn't trigger

      controller.setKeyPressed(
        const InputKeyId.gamepadButton('a', deviceId: 'pad-1'),
        false,
      );

      // Press prefix key, then regular key
      controller.setKeyPressed(
        const InputKeyId.gamepadButton('start', deviceId: 'pad-1'),
        true,
      );
      controller.setKeyPressed(
        const InputKeyId.gamepadButton('a', deviceId: 'pad-1'),
        true,
      );
      expect(fireCount, 1); // Prefix held, triggers!

      controller.setKeyPressed(
        const InputKeyId.gamepadButton('a', deviceId: 'pad-1'),
        false,
      );
      controller.setKeyPressed(
        const InputKeyId.gamepadButton('start', deviceId: 'pad-1'),
        false,
      );

      // Prefix negated: prefix key P (start) must NOT be pressed
      final bindingPrefixNegated = CustomShortcutBinding(
        actionId: 'test_action',
        prefixKey: const InputKeyId.gamepadButton('start'),
        prefixNegated: true,
        regularKeys: [const InputKeyId.gamepadButton('a'), null, null, null],
      );
      await controller.saveCustomBinding(bindingPrefixNegated);

      // Press prefix key first, then regular key
      controller.setKeyPressed(
        const InputKeyId.gamepadButton('start', deviceId: 'pad-1'),
        true,
      );
      controller.setKeyPressed(
        const InputKeyId.gamepadButton('a', deviceId: 'pad-1'),
        true,
      );
      expect(fireCount, 1); // Prefix active, regular key shielded!

      controller.setKeyPressed(
        const InputKeyId.gamepadButton('a', deviceId: 'pad-1'),
        false,
      );
      controller.setKeyPressed(
        const InputKeyId.gamepadButton('start', deviceId: 'pad-1'),
        false,
      );

      // Press regular key alone (no prefix)
      controller.setKeyPressed(
        const InputKeyId.gamepadButton('a', deviceId: 'pad-1'),
        true,
      );
      expect(fireCount, 2); // Prefix not active, triggers!

      controller.dispose();
    },
  );

  test('gamepad trigger axes map to button events with hysteresis debouncing', () {
    final controller = InputEventController();
    var fireCount = 0;

    controller.registerShortcutAction(
      ShortcutAction(
        id: 'test_action',
        descriptionKey: 'testAction',
        onTrigger: () => fireCount++,
      ),
    );

    final bindingLT = CustomShortcutBinding(
      actionId: 'test_action',
      regularKeys: [
        const InputKeyId.gamepadButton('leftTrigger'),
        null,
        null,
        null,
      ],
    );
    controller.saveCustomBinding(bindingLT);

    // Send leftTrigger axis value = 0.5 (below press threshold 0.6)
    controller.setAxisValue(
      const InputAxisId.gamepadAxis('leftTrigger', deviceId: 'pad-1'),
      0.5,
    );
    expect(fireCount, 0);

    // Send leftTrigger axis value = 0.7 (exceeds press threshold 0.6) -> Pressed
    controller.setAxisValue(
      const InputAxisId.gamepadAxis('leftTrigger', deviceId: 'pad-1'),
      0.7,
    );
    expect(fireCount, 1);

    // Send leftTrigger axis value = 0.4 (drops below 0.6, but above release threshold 0.3) -> Still Pressed!
    controller.setAxisValue(
      const InputAxisId.gamepadAxis('leftTrigger', deviceId: 'pad-1'),
      0.4,
    );
    expect(fireCount, 1); // No new press event, and no release (remains active)

    // Send leftTrigger axis value = 0.2 (drops below release threshold 0.3) -> Released
    controller.setAxisValue(
      const InputAxisId.gamepadAxis('leftTrigger', deviceId: 'pad-1'),
      0.2,
    );

    // Press again to verify release occurred
    controller.setAxisValue(
      const InputAxisId.gamepadAxis('leftTrigger', deviceId: 'pad-1'),
      0.8,
    );
    expect(fireCount, 2);

    controller.dispose();
  });
}
