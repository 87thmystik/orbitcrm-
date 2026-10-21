window.pipelineInterop = {
    instances: [],

    init: function (dotNetRef) {
        this.dispose();

        const columns = document.querySelectorAll('[data-pipeline-stage]');
        const self = this;

        columns.forEach((column) => {
            const sortable = Sortable.create(column, {
                group: 'pipeline',
                animation: 150,
                ghostClass: 'opacity-40',
                dragClass: 'rotate-1',
                onEnd: function (evt) {
                    const dealId = evt.item.getAttribute('data-deal-id');
                    const targetStageId = evt.to.getAttribute('data-pipeline-stage');
                    const sameColumn = evt.from === evt.to;

                    if (sameColumn) return;

                    // Revert Sortable's DOM move so Blazor's re-render
                    // (triggered by OnDealMoved) is the single source of truth.
                    if (evt.from && evt.item) {
                        const refNode = evt.from.children[evt.oldIndex] || null;
                        evt.from.insertBefore(evt.item, refNode);
                    }

                    if (dealId && targetStageId) {
                        dotNetRef.invokeMethodAsync('OnDealMoved', dealId, targetStageId)
                            .catch(err => console.error('[pipeline] OnDealMoved failed', err));
                    }
                }
            });

            self.instances.push(sortable);
        });
    },

    dispose: function () {
        this.instances.forEach((s) => s.destroy());
        this.instances = [];
    }
};