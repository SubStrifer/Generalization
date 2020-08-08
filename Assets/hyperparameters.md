### PPO
1. Reward Signals
2. Lambda `lambd` (0.9 - 0.95) - how much to relay on the current state
3. `buffer_size` (2048 - 409600) - how many experiences before learning, multiple of `batch_size`
4. `batch_size` (Action space: Continuous: 512 - 5120, Discrete: 32 - 512)
5. Number of Epochs `num_epoch` (3 - 10) - can go larger together with `batch_size`, decreasing will ensure stable updates but slower learning
6. `learning_rate` (1e-5 - 1e-3) - should be decreased if training is unstable and the reward does not consistently increase
7. (Optional) `learning_rate_schedule` (linear, constant) - for PPO linear is recommended
8. `time_horizon` - (32 - 2048) - experiences collected per-agent, more biased when short
9. `max_steps` (5e5 - 1e7)
10. `beta` (1e-4 - 1e-2) - strength of the entropy regualarization, which makes the policy "more random". Increasing this will ensure more random actions. If entropy drops too quickly (alongside reward increase), increase. If drops too slowly, decrease.
11. `epsilon` (0.1 - 0.3) - small value will result in more stable updates and slow training process
12. `normalize` - may be good for complex continuous control problems
13. Number of Layers `num_layers` (1 - 3) - how many hidden layers
14. `hidden_units` (32 - 512)
15. (Optional) Visual encoder type `vis_encode_type` (simple, nature_cnn, resnet)

### RNN
16. `sequence_length` (4 - 128)
17. `memory_size` (64 - 512)

### SAC

### Training Statistics
1. Cumulative Reward - should consistently increase over time. Small ups and downs are to be expected.
2. Entropy - should consistently decrease, adjust `beta`
3. Learning Rate - depends on `learning_rate_schedule`
4. Policy Loss - oscillate during training, should be less than 1.0
5. Value Estimate - should increase as the cumulative reward increases
6. Value Loss - increases with the reward, and then should decrease once reward become stable